namespace CryptoGains

open System
open System.Net.Http
open Oryx
open Oryx.ThothJsonNet.ResponseReader
open Thoth.Json.Net
open FSharp.Control.Tasks.V2.ContextInsensitive

[<RequireQualifiedAccess>]
module Transactions =
    let private baseUrl = "https://api.bitpanda.com/v1"

    let private apiKey =
        Environment.GetEnvironmentVariable("BITPANDA")

    let private tradeDecoder: Decoder<Trade list> =
        let tradeTypeDecoder =
            Decode.string
            |> Decode.map (fun str ->
                Enum.Parse(typeof<TradeType>, str, true) :?> TradeType
            )

        let tradeDecoder =
            Decode.object (fun get ->
                { Trade.Amount = get.Required.Field "amount_cryptocoin" Decode.decimal
                  Price = get.Required.Field "price" Decode.decimal
                  Type = get.Required.Field "type" tradeTypeDecoder })

        let attrDecoder = Decode.field "attributes" tradeDecoder

        let arryDecoder = Decode.list attrDecoder

        Decode.field "data" arryDecoder

    let private request resource =
        GET
        >=> withUrlBuilder (fun (req: HttpRequest) -> $"{baseUrl}/{resource}")
        >=> withHeader "X-API-KEY" apiKey
        >=> withQuery [ struct ("page_size", "100") ]
        >=> fetch
        >=> json tradeDecoder

    let getAllTrades () =
        task {
            use client = new HttpClient()

            let ctx =
                Context.defaultContext
                |> Context.withHttpClient client

            let! result = request "trades" |> runAsync ctx

            printfn "%A" result

            return result
        }
