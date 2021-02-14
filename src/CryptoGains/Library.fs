namespace CryptoGains

open System.Net.Http
open FSharp.Control.Tasks.V2.ContextInsensitive

open Oryx
open Oryx.ThothJsonNet.ResponseReader
open Thoth.Json.Net

// {"cardano":{"eur":0.760946,"usd":0.921155},"litecoin":{"eur":151.5,"usd":183.4}}

type Coin = { Name: string; Prices: Map<string, decimal> }

module CryptoGains =
    [<Literal>]
    let private url = ""

    let coinDecoder: Decoder<Coin seq> =
        let priceDecoder = Decode.dict Decode.decimal            

        Decode.dict priceDecoder
        |> Decode.map
            (Seq.map (fun kvp ->
                { Coin.Name = kvp.Key
                  Prices = kvp.Value }))


    let private query coins currencies =
        let coins = coins |> String.concat ","
        let currencies = currencies |> String.concat ","

        [ struct ("ids", coins)
          struct ("vs_currencies", currencies) ]

    let private request coins currencies =
        GET
        >=> withUrl url
        >=> withQuery (query coins currencies)
        >=> fetch
        >=> json coinDecoder 
            

    let getPrices coins currencies =
        task {
            use client = new HttpClient()

            let ctx =
                Context.defaultContext
                |> Context.withHttpClient client

            let! result = request coins currencies |> runAsync ctx

            return result
        }
