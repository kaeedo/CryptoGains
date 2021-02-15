namespace CryptoGains

open System
open System.Net.Http
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling.Operator.TaskResult
open Oryx
open Oryx.ThothJsonNet.ResponseReader
open Thoth.Json.Net

[<RequireQualifiedAccess>]
module Transactions =
    let private baseBitpandaUrl = "https://api.bitpanda.com/v1"

    let private baseCoinGeckoUrl = "https://api.coingecko.com/api/v3"

    let private apiKey =
        Environment.GetEnvironmentVariable("BITPANDA")

    let private tradeDecoder (bitpandaCoins: Map<int, string>) (coinGeckoCoins: Map<string, string * string>): Decoder<Trade list> =
        let tradeTypeDecoder =
            Decode.string
            |> Decode.map (fun str -> Enum.Parse(typeof<TradeType>, str, true) :?> TradeType)

        let tradeDecoder =
            Decode.object (fun get ->
                let symbol =
                    (bitpandaCoins.[get.Required.At [ "attributes"; "cryptocoin_id" ] Decode.int]).ToUpperInvariant()
                    
                { Trade.Amount = get.Required.At [ "attributes"; "amount_cryptocoin" ] Decode.decimal
                  Price = get.Required.At [ "attributes"; "price" ] Decode.decimal
                  Type = get.Required.At [ "attributes"; "type" ] tradeTypeDecoder
                  Cryptocoin =
                      { Cryptocoin.BitpandaId = get.Required.At [ "attributes"; "cryptocoin_id" ] Decode.int
                        CoinGeckoId = coinGeckoCoins.[symbol] |> fst
                        Symbol = symbol
                        Name = coinGeckoCoins.[symbol] |> snd }

                })

        Decode.field "data" (Decode.list tradeDecoder)

    let private coinIdDecoder: Decoder<Map<int, string>> =
        let objDecoder =
            Decode.object (fun get ->
                (get.Required.At [ "attributes"; "cryptocoin_id" ] Decode.int),
                (get.Required.At [ "attributes"; "cryptocoin_symbol" ] Decode.string))

        let listDecoder = Decode.list objDecoder
        let data = Decode.field "data" listDecoder
        data |> Decode.map (Map.ofList)

    let private coinGeckoCoinListDecoder: Decoder<Map<string, string * string>> =
        let objDecoder =
            Decode.object (fun get ->
                (get.Required.Field "symbol" Decode.string)
                    .ToUpperInvariant(),
                ((get.Required.Field "id" Decode.string), (get.Required.Field "name" Decode.string)))

        Decode.list objDecoder
        |> Decode.map (Map.ofList)
        
    let private coinPriceDecoder: Decoder<Map<string, decimal>> =
        Decode.dict <| Decode.field "EUR" Decode.decimal

    let private bitpandaRequest resource decoder =
        GET
        >=> withUrlBuilder (fun (req: HttpRequest) -> $"{baseBitpandaUrl}/{resource}")
        >=> withHeader "X-API-KEY" (if resource = "ticker" then String.Empty else apiKey)
        >=> withQuery [ struct ("page_size", "100") ]
        >=> fetch
        >=> json decoder

    let private coinGeckoRequest resource query decoder =
        GET
        >=> withUrlBuilder (fun _ -> $"{baseCoinGeckoUrl}/{resource}")
        >=> withQuery query
        >=> fetch
        >=> json decoder
    // coins/list
    // /simple/price

    let getAllTransactions () =
        taskResult {
            use client = new HttpClient()

            let ctx =
                Context.defaultContext
                |> Context.withHttpClient client

            let! cryptocoinIdMap =
                bitpandaRequest "wallets" coinIdDecoder
                |> runAsync ctx

            and! coinList = coinGeckoRequest "coins/list" [] coinGeckoCoinListDecoder |> runAsync ctx

            let! trades =
                bitpandaRequest "trades" (tradeDecoder cryptocoinIdMap coinList)
                |> runAsync ctx

            return trades
        }
    
    let getCurrentPrices symbols =
        taskResult {
            use client = new HttpClient()

            let ctx =
                Context.defaultContext
                |> Context.withHttpClient client
            
            let! prices = bitpandaRequest "ticker" coinPriceDecoder |> runAsync ctx
            
            let prices =
                symbols
                |> Seq.map (fun s ->
                    s, prices.[s]
                    )
                |> Map.ofSeq
            
            return prices
        }