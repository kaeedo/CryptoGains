open CryptoGains
open Spectre.Console

[<EntryPoint>]
let main argv =
    printfn "%A" <| Transactions.getAllTrades().GetAwaiter().GetResult()
    let wantedCoins =
        [ "cardano"
          "bitpanda-ecosystem-token"
          "ethereum"
          "litecoin" ]

    let wantedCurrencies = [ "eur"; "usd" ]

    let result =
        (CryptoGains.getPrices wantedCoins wantedCurrencies)
            .GetAwaiter()
            .GetResult()
            
    // get transactions
    // calculate price per unit per coin
    // get current prices
    // calculate percent gain
    // render

    match result with
    | Error e -> printfn "An error occured: %A" e
    | Ok r ->
        let table = Table()
        table.Border <- TableBorder.Minimal

        table.AddColumn("Coin") |> ignore

        wantedCurrencies
        |> List.iter (table.AddColumn >> ignore)

        r
        |> Seq.iter (fun coin ->
            let values =
                [| coin.Name

                   yield!
                       coin.Prices
                       |> Seq.map (fun kvp -> kvp.Value.ToString()) |]

            table.AddRow(values) |> ignore)

        // Render the table to the console
        AnsiConsole.Render(table)

    0 // return an integer exit code
