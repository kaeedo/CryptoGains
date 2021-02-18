open CryptoGains
open Spectre.Console
open FsToolkit.ErrorHandling
open Spectre.Console
open Spectre.Console.Rendering

[<EntryPoint>]
let main argv =
    let run =
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

                
            let totalCurrentPriceColumn = TableColumn("Total Current Price")
            totalCurrentPriceColumn.Footer <- Text($"{totalCurrentPrice:N2}") :> IRenderable

            let totalPercentChange = ((totalCurrentPrice / totalPricePaid) - 1M) * 100M
            let percentChangeColumn = TableColumn("Percent Change")
            percentChangeColumn.Footer <- Text($"{totalPercentChange:N2}%%") :> IRenderable

            table.AddColumn("Coin") |> ignore
            table.AddColumn("Amount Owned") |> ignore
            table.AddColumn(totalPricePaidColumn) |> ignore
            table.AddColumn(totalCurrentPriceColumn) |> ignore
            table.AddColumn(percentChangeColumn) |> ignore

            [ 1 .. 4 ]
            |> Seq.iter (fun i -> table.Columns.[i].RightAligned() |> ignore)

            assets
            |> Seq.iter (fun r ->
                let totalCurrentPrice =
                    let currentPrice = currentPrices.[r.Cryptocoin.Symbol]
                    currentPrice * r.AmountOwned

                let color = ""

                let percentChange =
                    ((totalCurrentPrice / r.PricePaid) - 1M) * 100M

                table.AddRow
                    ([| Text($"{r.Cryptocoin.Symbol}") :> IRenderable
                        Text($"{r.AmountOwned}") :> IRenderable
                        Text($"{r.PricePaid:N2}") :> IRenderable
                        Text($"{totalCurrentPrice:N2}") :> IRenderable
                        Text($"{percentChange:N2}%%") :> IRenderable |])
                |> ignore)

            AnsiConsole.Render(table))
        |> TaskResult.mapError (fun e -> printfn "An error has occured: %A" e)

    let status = AnsiConsole.Status()
    status.Spinner <- Spinner.Known.Dots2
    
    status
        .StartAsync("Getting BitPanda data...", fun _ -> run)
        .GetAwaiter().GetResult()
        |> ignore

    0 // return an integer exit code
