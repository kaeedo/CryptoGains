namespace CryptoGains

open System
open System.Net.Http
open System.Threading.Tasks
open Oryx
open Oryx.ThothJsonNet.ResponseReader
open Thoth.Json.Net
open FSharp.Control.Tasks.V2.ContextInsensitive
open Railways

[<RequireQualifiedAccess>]
module Transactions =
    let private baseBitpandaUrl = "https://api.bitpanda.com/v1"

    let private baseCoinGeckoUrl = "https://api.coingecko.com/api/v3"

    let private apiKey =
        Environment.GetEnvironmentVariable("BITPANDA")

    let private cryptocoinDecoder: Decoder<Cryptocoin list> =
        let objectDecoder =
            Decode.object (fun get ->
                { Cryptocoin.Id = get.Required.Field "id" Decode.string
                  Symbol = get.Required.Field "symbol" Decode.string
                  Name = get.Required.Field "name" Decode.string })

        Decode.list objectDecoder

    let private tradeDecoder cryptocoins: Decoder<Trade list> =
        let tradeTypeDecoder =
            Decode.string
            |> Decode.map (fun str -> Enum.Parse(typeof<TradeType>, str, true) :?> TradeType)

        let tradeDecoder =
            Decode.object (fun get ->
                { Trade.Amount = get.Required.At [ "attributes"; "amount_cryptocoin" ] Decode.decimal
                  Price = get.Required.At [ "attributes"; "price" ] Decode.decimal
                  Type = get.Required.At [ "attributes"; "type" ] tradeTypeDecoder
                  Cryptocoin =
                      cryptocoins
                      |> List.find (fun c ->
                          c.Symbol.ToLowerInvariant() = get.Required.At [ "attributes"; "cryptocoin_id" ] Decode.string) })

        Decode.field "data" (Decode.list tradeDecoder)

    let private bitpandaRequest resource =
        GET
        >=> withUrlBuilder (fun (req: HttpRequest) -> $"{baseBitpandaUrl}/{resource}")
        >=> withHeader "X-API-KEY" apiKey
        >=> withQuery [ struct ("page_size", "100") ]
        >=> fetch
    // >=> json (tradeDecoder cryptocoins)

    let private coinGeckoRequest resource =
        GET
        >=> withUrlBuilder (fun _ -> $"{baseCoinGeckoUrl}/{resource}")
        >=> fetch
    // coins/list
    // /simple/price

    let getAllTrades () =
        task {
            use client = new HttpClient()

            let ctx =
                Context.defaultContext
                |> Context.withHttpClient client

            let! results =
                Task.WhenAll
                    ([ bitpandaRequest "trades" |> runAsync ctx
                       bitpandaRequest "wallets" |> runAsync ctx
                       coinGeckoRequest "coins/lis" |> runAsync ctx ])

            let! cryptocoins =
                task {
                    match results.[0] with
                    | Ok r ->
                        let! str = r.ReadAsStringAsync()
                        let cc = Decode.fromString cryptocoinDecoder str
                        return cc
                }

            let coinIdDecoder: Decoder<Map<int, string>> =
                let objDecoder =
                    Decode.object (fun get ->
                        (get.Required.At [ "attributes"; "cryptocoin_id" ] Decode.int),
                        (get.Required.At [ "attributes"; "cryptocoin_symbol" ] Decode.string))

                let listDecoder = Decode.list objDecoder
                let data = Decode.field "data" listDecoder
                data |> Decode.map (Map.ofList)

            let! bitpandaCoinIds =
                task {
                    match results.[1] with
                    | Ok r ->
                        let! str = r.ReadAsStringAsync()
                        let w = Decode.fromString coinIdDecoder str
                        // TODO: Combine with cryptocoins on line 76
                        return w
                }

            let! trades =
                task {
                    match results.[2] with
                    | Ok r -> return! r.ReadAsStringAsync()
                        // TODO: Combine with bitpandaCoinIds on line 95
                }
                
            // TODO Railwayify. maybe with async railways

            printfn "%A" trades

            return trades
        }
