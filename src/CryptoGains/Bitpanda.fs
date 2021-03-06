﻿namespace CryptoGains

open System
open System.Net.Http
open FsToolkit.ErrorHandling
open Oryx
open Oryx.ThothJsonNet.ResponseReader
open Thoth.Json.Net

[<RequireQualifiedAccess>]
module Bitpanda =
    let private baseBitpandaUrl = "https://api.bitpanda.com/v1"

    let masterDataDecoder =
        let currencies =
            let currencyObjDecoder =
                Decode.object (fun get ->
                    let id = get.Required.Field "id" Decode.int

                    let name =
                        get.Required.At [ "attributes"; "name" ] Decode.string

                    let symbol =
                        get.Required.At [ "attributes"; "symbol" ] Decode.string

                    let toEuroRate =
                        get.Required.At [ "attributes"; "to_eur_rate" ] Decode.decimal

                    let hasWallets =
                        get.Required.At [ "attributes"; "has_wallets" ] Decode.bool

                    { Currency.Id = id
                      Name = name
                      Symbol = symbol
                      ToEurRate = toEuroRate },
                    hasWallets)

            Decode.list currencyObjDecoder

        let cryptocoins =
            let cryptocoinObjDecoder =
                Decode.object (fun get ->
                    let id = get.Required.Field "id" Decode.int

                    let name =
                        get.Required.At [ "attributes"; "name" ] Decode.string

                    let symbol =
                        get.Required.At [ "attributes"; "symbol" ] Decode.string

                    { Cryptocoin.Id = id
                      Name = name
                      Symbol = symbol })

            Decode.list cryptocoinObjDecoder

        Decode.object (fun get ->
            let currencies =
                get.Required.At [ "data"; "attributes"; "fiats" ] currencies

            let currencies =
                currencies |> List.filter (snd) |> List.map (fst)

            let cryptocoins =
                get.Required.At [ "data"; "attributes"; "cryptocoins" ] cryptocoins

            { MasterData.Currencies = currencies
              Cryptocoins = cryptocoins })


    let private tradeDecoder (masterData: MasterData): Decoder<Trade list> =
        let tradeTypeDecoder =
            Decode.string
            |> Decode.map (function
                | "buy" -> Buy
                | "sell" -> Sell
                | "withdrawal" -> Withdrawal
                | _ -> TradeType.Unsupported)

        let tradeDecoder =
            Decode.object (fun get ->
                let cryptocoin =
                    let cryptocoinId =
                        get.Required.At [ "attributes"; "cryptocoin_id" ] Decode.int

                    masterData.Cryptocoins
                    |> List.find (fun c -> c.Id = cryptocoinId)

                let fiatCurrency =
                    let fiatId =
                        get.Required.At [ "attributes"; "current_fiat_id" ] Decode.int

                    masterData.Currencies
                    |> List.find (fun c -> c.Id = fiatId)

                let fiatCurrency =
                    { AmountPaid.Currency = fiatCurrency
                      Amount = get.Required.At [ "attributes"; "current_fiat_amount" ] Decode.decimal
                      AmountEur = get.Required.At [ "attributes"; "amount_eur" ] Decode.decimal }

                { Trade.Amount = get.Required.At [ "attributes"; "amount" ] Decode.decimal
                  AmountPaid = fiatCurrency
                  Type = get.Required.At [ "attributes"; "type" ] tradeTypeDecoder
                  Cryptocoin = cryptocoin })

        Decode.field "data" (Decode.list tradeDecoder)

    let private coinPriceDecoder (masterData: MasterData): Decoder<Map<int, Map<string, decimal>>> =
        let currencyValueDecoder = Decode.dict Decode.decimal

        Decode.dict currencyValueDecoder
        |> Decode.map (fun dict ->
            dict
            |> Seq.map (fun kvp ->
                let symbol = kvp.Key

                let coin =
                    masterData.Cryptocoins
                    |> List.tryFind (fun c -> c.Symbol = symbol)

                coin, kvp.Value)
            |> Seq.filter (fun (coin, _) -> coin.IsSome)
            |> Seq.map (fun (coin, prices) -> coin.Value.Id, prices)
            |> Map.ofSeq)

    let getMasterData () =
        taskResult {
            use client = new HttpClient()

            let ctx =
                Context.defaultContext
                |> Context.withHttpClient client
                
            let! masterData =
                GET
                >=> withUrlBuilder (fun (req: HttpRequest) -> $"{baseBitpandaUrl}/masterdata")
                >=> fetch
                >=> json masterDataDecoder
                |> runAsync ctx

            return masterData
        }

    let getAllTransactions masterData apiKey =
        taskResult {
            use client = new HttpClient()

            let ctx =
                Context.defaultContext
                |> Context.withHttpClient client
            
            let! trades =
                GET
                >=> withUrlBuilder (fun (req: HttpRequest) -> $"{baseBitpandaUrl}/wallets/transactions")
                >=> withHeader "X-API-KEY" apiKey
                >=> withQuery [ struct ("page_size", "100"); struct ("status", "finished") ]
                >=> fetch
                >=> json (tradeDecoder masterData)   
                |> runAsync ctx

            return trades
        }

    let getCurrentPrices masterData =
        taskResult {
            use client = new HttpClient()

            let ctx =
                Context.defaultContext
                |> Context.withHttpClient client

            let! prices =
                GET
                >=> withUrlBuilder (fun (req: HttpRequest) -> $"{baseBitpandaUrl}/ticker")
                >=> fetch
                >=> json (coinPriceDecoder masterData)
                |> runAsync ctx

            return prices
        }
