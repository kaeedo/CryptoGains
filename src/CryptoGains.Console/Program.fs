open System
open CryptoGains
open CryptoGains.Console
open Spectre.Console
open FsToolkit.ErrorHandling
open Spectre.Console.Rendering

[<EntryPoint>]
let main argv =
    let run () =
        taskResult {
            let! transactions = Bitpanda.getAllTransactions ()
            let! assets = Assets.getAssets transactions

            let! currentPrices =
                Bitpanda.getCurrentPrices
                    (assets
                     |> List.map (fun a -> a.Cryptocoin.Symbol))

            return assets, currentPrices
        }
        |> TaskResult.map (fun (assets, currentPrices) ->
            let table = Table()
            table.Border <- TableBorder.SimpleHeavy

            let totalPricePaid = assets |> Seq.sumBy (fun r -> r.PricePaid)
            let totalPricePaidColumn = TableColumn("Total Price Paid")
            totalPricePaidColumn.Footer <- Text($"{totalPricePaid:N2}") :> IRenderable

            let totalCurrentPrice =
                assets
                |> Seq.sumBy (fun a ->
                    let amountOwned = a.AmountOwned
                    let currentPrice = currentPrices.[a.Cryptocoin.Symbol]
                    amountOwned * currentPrice)

                
            let totalCurrentPriceColumn = TableColumn("Current Value")
            totalCurrentPriceColumn.Footer <- Text($"{totalCurrentPrice:N2}") :> IRenderable

            let totalPercentChange =
                let pct = ((totalCurrentPrice / totalPricePaid) - 1M) * 100M
                if pct >0.0M
                then $"[green]{pct:N2}%%[/]"
                else $"[red]{pct:N2}%%[/]"
                
            let percentChangeColumn = TableColumn("Change")
            let difference =
                    let diff = (totalCurrentPrice - totalPricePaid)
                    let color =
                        if diff > 0.0M
                        then "green"
                        else "red"
                    $"[{color}]{diff:N2}[/]"
                    
            percentChangeColumn.Footer <- Markup($"{difference} ({totalPercentChange:N2})") :> IRenderable

            table.AddColumn("Coin") |> ignore
            table.AddColumn("Amount Owned") |> ignore
            table.AddColumn(totalPricePaidColumn) |> ignore
            table.AddColumn(totalCurrentPriceColumn) |> ignore
            table.AddColumn(percentChangeColumn) |> ignore

            [ 1 .. 4 ]
            |> Seq.iter (fun i -> table.Columns.[i].RightAligned() |> ignore)
            table.Columns.[4].PadLeft(5) |> ignore
            
            let longest =
                assets
                |> Seq.map (fun a ->
                    let totalCurrentPrice =
                        let currentPrice = currentPrices.[a.Cryptocoin.Symbol]
                        currentPrice * a.AmountOwned
                        
                    (int <| ((totalCurrentPrice / a.PricePaid) - 1M) * 100M).ToString().Length
                    )
                |> Seq.max

            assets
            |> Seq.iter (fun r ->
                let totalCurrentPrice =
                    let currentPrice = currentPrices.[r.Cryptocoin.Symbol]
                    currentPrice * r.AmountOwned

                let percentChange =
                    let pct = ((totalCurrentPrice / r.PricePaid) - 1M) * 100M
                    let color =
                        if pct > 0.0M
                        then "green"
                        else "red"
                    $"[{color}]{pct:N2}%%[/]"
                    
                let difference =
                    let diff = (totalCurrentPrice - r.PricePaid)
                    let color =
                        if diff > 0.0M
                        then "green"
                        else "red"
                    $"[{color}]{diff:N2}[/]"
                    
                let padding =                        
                    let amountNeeded =
                        let currentLength =
                            (int <| ((totalCurrentPrice / r.PricePaid) - 1M) * 100M).ToString().Length
                        longest - currentLength + 1
                        
                    String.replicate amountNeeded " "
                    
                let hasNotes =
                    if r.Properties |> List.contains (Property.HasExternalAmount)
                    then "*"
                    else String.Empty

                table.AddRow
                    ([| Text($"{hasNotes} {r.Cryptocoin.Symbol}") :> IRenderable
                        Text($"{r.AmountOwned}") :> IRenderable
                        Text($"{r.PricePaid:N2}") :> IRenderable
                        Text($"{totalCurrentPrice:N2}") :> IRenderable
                        Markup($"{difference}{padding}({percentChange:N2})") :> IRenderable |])
                |> ignore)

            AnsiConsole.Render(table)

            if assets |> Seq.exists (fun a -> a.Properties |> List.contains (Property.HasExternalAmount))
            then
                AnsiConsole.WriteLine("Items denoted with (*) may have some or all coins held externally, and may not accurately be represented here.")

            // TODO communicate multi currency
            )
        
            
        |> TaskResult.mapError (fun e -> printfn "An error has occured: %A" e)

    let status = AnsiConsole.Status()
    status.Spinner <- Spinner.Known.Dots2
    
    status
        .StartAsync("Getting BitPanda data...", fun _ -> run ())
        .GetAwaiter().GetResult()
        |> ignore

    0 // return an integer exit code

