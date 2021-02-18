namespace CryptoGains

open System
open System.Net.Http
open FsToolkit.ErrorHandling
open Oryx
open Oryx.ThothJsonNet.ResponseReader
open Thoth.Json.Net

[<RequireQualifiedAccess>]
module Bitpanda =
    let private baseBitpandaUrl = "https://api.bitpanda.com/v1"

    let private apiKey =
        Environment.GetEnvironmentVariable("BITPANDA")
        
    let private tradeDecoder (bitpandaCoins: Map<int, string>): Decoder<Trade list> =
        let tradeTypeDecoder =
            Decode.string
            |> Decode.map (function
                            | "buy" -> Buy
                            | "sell" -> Sell
                            | "withdrawal" -> Withdrawal)

        let tradeDecoder =
            Decode.object (fun get ->
                let symbol =
                    (bitpandaCoins.[get.Required.At [ "attributes"; "cryptocoin_id" ] Decode.int]).ToUpperInvariant()
                    
                { Trade.Amount = get.Required.At [ "attributes"; "amount" ] Decode.decimal
                  AmountPaid = get.Required.At [ "attributes"; "current_fiat_amount" ] Decode.decimal
                  Type = get.Required.At [ "attributes"; "type" ] tradeTypeDecoder
                  Cryptocoin =
                      { Cryptocoin.Id = get.Required.At [ "attributes"; "cryptocoin_id" ] Decode.int
                        Symbol = symbol }

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
        
    let private coinPriceDecoder: Decoder<Map<string, decimal>> =
        Decode.dict <| Decode.field "EUR" Decode.decimal
        
    let private bitpandaRequest resource (query: struct(string * string) list) decoder =
        let query = struct ("page_size", "100") :: query
        
        GET
        >=> withUrlBuilder (fun (req: HttpRequest) -> $"{baseBitpandaUrl}/{resource}")
        >=> withHeader "X-API-KEY" (if resource = "ticker" then String.Empty else apiKey)
        >=> withQuery query
        >=> fetch
        >=> json decoder
        
    let getAllTransactions () =
        taskResult {
            use client = new HttpClient()

            let ctx =
                Context.defaultContext
                |> Context.withHttpClient client

            let! cryptocoinIdMap =
                bitpandaRequest "wallets" [] coinIdDecoder
                |> runAsync ctx

            let! trades =
                bitpandaRequest "wallets/transactions" [struct ("status", "finished")] (tradeDecoder cryptocoinIdMap)
                |> runAsync ctx

            return trades
        }
        
    let getCurrentPrices symbols =
        taskResult {
            use client = new HttpClient()

            let ctx =
                Context.defaultContext
                |> Context.withHttpClient client
            
            let! prices = bitpandaRequest "ticker" [] coinPriceDecoder |> runAsync ctx
            
            let prices =
                symbols
                |> Seq.map (fun s ->
                    s, prices.[s]
                    )
                |> Map.ofSeq
            
            return prices
        }