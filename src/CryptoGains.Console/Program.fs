open System
open System.Globalization
open CryptoGains
open CryptoGains.Console
open Spectre.Console
open FsToolkit.ErrorHandling
open Spectre.Console.Rendering

[<EntryPoint>]
let main argv =
    Console.OutputEncoding <- System.Text.Encoding.UTF8
    
    let getData () =
        taskResult {
            let! masterData = Bitpanda.getMasterData ()
            
            let! transactions = Bitpanda.getAllTransactions masterData
            and! currentPrices = Bitpanda.getCurrentPrices masterData

            let! assets = Assets.getAssets masterData transactions 
                    
            return assets, currentPrices
        }
    
    let run (fetchResult: Result<(Asset list * Map<int, Map<string, decimal>>), Oryx.HandlerError<obj>>) =
        taskResult {
            let! (assets, currentPrices) = fetchResult
            let availableCoins = currentPrices |> Seq.map (fun kvp -> kvp.Key) |> Seq.toArray
            
            let assets =
                assets
                |> List.map (Wizard.confirmAssetAmount)
                |> List.append (Wizard.addAdditionalCoins availableCoins)

            return assets, currentPrices
        }
        |> TaskResult.map (fun (assets, currentPrices) ->
            let table = Table()
            table.Border <- TableBorder.SimpleHeavy

            let totalPricePaid = assets |> Seq.sumBy (fun r -> snd r.PricePaid)
            let totalPricePaidColumn = TableColumn("Total Price Paid")
            
            // TODO fix culture
            totalPricePaidColumn.Footer <- Text(totalPricePaid.ToString("c", CultureInfo.CurrentCulture)) :> IRenderable

            let totalCurrentPrice =
                assets
                |> Seq.sumBy (fun a ->
                    let amountOwned = a.AmountOwned
                    let currentPrice = currentPrices.[a.Cryptocoin.Id].["EUR"] // TODO Multicurrency
                    amountOwned * currentPrice)
                
            let totalCurrentPriceColumn = TableColumn("Current Value")
            // TODO fix culture
            totalCurrentPriceColumn.Footer <- Text(totalCurrentPrice.ToString("c", CultureInfo.CurrentCulture)) :> IRenderable

            let totalPercentChange =
                let pct = ((totalCurrentPrice / totalPricePaid) - 1M) * 100M
                if pct > 0.0M
                then $"[green]{pct:N2}%%[/]"
                else $"[red]{pct:N2}%%[/]"
                
            let percentChangeColumn = TableColumn("Change")
            let difference =
                    let diff = (totalCurrentPrice - totalPricePaid)
                    let color =
                        if diff > 0.0M
                        then "green"
                        else "red"
                    let diff = diff.ToString("c", CultureInfo.CurrentCulture)
                    $"[{color}]{diff}[/]"
                    
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
                        let currentPrice = currentPrices.[a.Cryptocoin.Id].["EUR"] // TODO Multicurrency
                        currentPrice * a.AmountOwned
                        
                    (int <| ((totalCurrentPrice / (snd a.PricePaid)) - 1M) * 100M).ToString().Length
                    )
                |> Seq.max

            assets
            |> Seq.iter (fun r ->
                let totalCurrentPrice =
                    let currentPrice = currentPrices.[r.Cryptocoin.Id].["EUR"] // TODO Multicurrency
                    currentPrice * r.AmountOwned
                    
                let culture =
                    let culture =
                        if r.Properties |> List.contains(Property.IsMultiCurrency)
                        then "de-DE"
                        else
                            match fst r.PricePaid with
                            | { Currency.Id = _; Symbol = "EUR"; Name = _} -> "de-DE"
                            | { Currency.Id = _; Symbol = "USD"; Name = _} -> "en-US"
                            | { Currency.Id = _; Symbol = "CHF"; Name = _} -> "de-CH"
                            | { Currency.Id = _; Symbol = "GBP"; Name = _} -> "en-GB"
                            | { Currency.Id = _; Symbol = "TRY"; Name = _} -> "tr-TR"
                            | _ -> "de-DE"
                    CultureInfo(culture)
                        
                let percentChange =
                    let pct = ((totalCurrentPrice / (snd r.PricePaid)) - 1M) * 100M
                    let color =
                        if pct > 0.0M
                        then "green"
                        else "red"
                    $"[{color}]{pct:N2}%%[/]"
                    
                let difference =
                    let diff = (totalCurrentPrice - (snd r.PricePaid))
                    let color =
                        if diff > 0.0M
                        then "green"
                        else "red"

                    let diff = diff.ToString("c", culture)
                    $"[{color}]{diff}[/]"
                   
                let padding =                        
                    let amountNeeded =
                        let currentLength =
                            (int <| ((totalCurrentPrice / (snd r.PricePaid)) - 1M) * 100M).ToString().Length
                        longest - currentLength + 1
                        
                    String.replicate amountNeeded " "
                    
                let hasNotes =
                    if r.Properties |> List.contains (Property.HasExternalAmount)
                    then "*"
                    else String.Empty
                    
                let pricePaid = (snd r.PricePaid).ToString("c", culture)
                let totalCurrentPrice = (totalCurrentPrice).ToString("c", culture)

                table.AddRow
                    ([| Text($"{hasNotes} {r.Cryptocoin.Symbol}") :> IRenderable
                        Text($"{r.AmountOwned}") :> IRenderable
                        Text($"{pricePaid}") :> IRenderable
                        Text($"{totalCurrentPrice}") :> IRenderable
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
    
    let fetchResult =
        status
            .StartAsync("Getting BitPanda data...", fun _ -> getData())
            .GetAwaiter().GetResult()
    
    run fetchResult |> ignore

    0 // return an integer exit code

