// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization
open Spectre.Console
open Oryx
open Oryx.SystemTextJson.ResponseReader
open FSharp.Control.Tasks.V2.ContextInsensitive

[<Literal>]
let Url =
    "https://api.coingecko.com/api/v3/simple/price"

[<JsonFSharpConverter>]
type Price = { Eur: float; Usd: float }

[<JsonFSharpConverter>]
type Response = Map<string, Price>

// JsonPushStreamContent
let options =
    let options =
        JsonSerializerOptions
            (AllowTrailingCommas = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, IgnoreNullValues = true)

    let converter = JsonFSharpConverter()
    //converter.CreateConverter (typeof<Response>, options)
    options.Converters.Add(converter)
    options

let query =
    [ struct ("ids", "cardano,bitpanda-ecosystem-token,ethereum,litecoin")
      struct ("vs_currencies", "eur,usd") ]

let request (): HttpHandler<HttpResponseMessage, _, Response, _> =
    GET
    >=> withUrl Url
    >=> withQuery (query)
    >=> fetch
    >=> json options

let asyncMain argv =
    task {
        use client = new HttpClient()

        let ctx =
            Context.defaultContext
            |> Context.withHttpClient client

        let! result = request () |> runAsync ctx

        return result 
    }

[<EntryPoint>]
let main argv =
    let result = asyncMain().GetAwaiter().GetResult()
    match result with
    | Error e -> printfn "An error occured: %A" e
    | Ok r ->
        let table = Table()
        table.Border <- TableBorder.Minimal

        table.AddColumn("Coin") |> ignore
        table.AddColumn("€") |> ignore
        table.AddColumn("$") |> ignore
        r
        |> Map.iter (fun key price ->
            table.AddRow(key, price.Eur.ToString(), price.Usd.ToString()) |> ignore
        ) 
        

        
//        table.AddColumn(TableColumn("Bagh")) |> ignore
//        table.AddColumn(TableColumn("Barstgh")) |> ignore
//
//        table.AddColumn(TableColumn("Barthsrtgh"))
//        |> ignore
//
//        // Add some rows
//        table.AddRow("Baz", "[green]Qux[/]", "erg", "gserg'", "feee")
//        |> ignore
//
//        table.AddRow(Markup("[blue]Corgi[/]"), Text("Waldo"))
//        |> ignore

        // Render the table to the console
        AnsiConsole.Render(table)
    0 // return an integer exit code
