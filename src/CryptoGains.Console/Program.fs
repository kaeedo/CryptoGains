open System
open System.Globalization
open CryptoGains
open CryptoGains.Console
open FsToolkit.ErrorHandling.Operator.TaskResult
open Spectre.Console
open FsToolkit.ErrorHandling
open Spectre.Console.Rendering

let getCulture =
    function
    | { Currency.Symbol = "EUR" } -> CultureInfo("de-DE")
    | { Currency.Symbol = "USD" } -> CultureInfo("en-US")
    | { Currency.Symbol = "CHF" } -> CultureInfo("de-CH")
    | { Currency.Symbol = "GBP" } -> CultureInfo("en-GB")
    | { Currency.Symbol = "TRY" } -> CultureInfo("tr-TR")
    | _ -> CultureInfo("de-DE")

let calculateTotalPricePaid (masterData: MasterData) (assets: Asset list) =
    let isMultiCurrency =
        (assets
         |> List.map (fun a -> fst a.PricePaid)
         |> List.distinct)
            .Length > 1

    if isMultiCurrency then
        let euro =
            masterData.Currencies
            |> List.find (fun c -> c.Symbol = "EUR")

        let assets =
            assets
            |> List.map (fun a ->
                let amountInEur =
                    let (currency, amountPaid) = a.PricePaid
                    amountPaid * currency.ToEurRate

                { a with
                      PricePaid = (euro, amountInEur) })

        getCulture euro, assets |> List.sumBy (fun a -> snd a.PricePaid)
    else
        let currency = fst (assets |> List.head).PricePaid
        getCulture currency, assets |> List.sumBy (fun a -> snd a.PricePaid)

let calculateTotalCurrentValue masterData (currentPrices: Map<int, Map<string, decimal>>) assets =
    let isMultiCurrency =
        (assets
         |> List.map (fun a -> fst a.PricePaid)
         |> List.distinct)
            .Length > 1

    if isMultiCurrency then
        let euro =
            masterData.Currencies
            |> List.find (fun c -> c.Symbol = "EUR")

        let totalCurrentValue =
            assets
            |> List.sumBy (fun a ->
                let amountOwned = a.AmountOwned
                let (currency, _) = a.PricePaid

                let currentPrice =
                    currentPrices.[a.Cryptocoin.Id].[currency.Symbol]

                amountOwned * currentPrice * currency.ToEurRate)

        getCulture euro, totalCurrentValue
    else
        let currency = fst (assets |> List.head).PricePaid

        let totalCurrentValue =
            assets
            |> List.sumBy (fun a ->
                let amountOwned = a.AmountOwned
                let (currency, _) = a.PricePaid

                let currentPrice =
                    currentPrices.[a.Cryptocoin.Id].[currency.Symbol]

                amountOwned * currentPrice)

        getCulture currency, totalCurrentValue

let calculatePercentChange current total =
    let pct = ((current / total) - 1M)

    let color = if pct >= 0.0M then "green" else "red"

    let pct =
        pct.ToString("P2", CultureInfo.CurrentCulture)

    $"[{color}]{pct}[/]"

[<EntryPoint>]
let main argv =
    Console.OutputEncoding <- System.Text.Encoding.UTF8

    let configuration =
        CryptoGains.Console.Configuration.Configuration()

    let getData apiKey =
        taskResult {
            let! masterData = Bitpanda.getMasterData ()

            let! transactions = Bitpanda.getAllTransactions masterData apiKey
            and! currentPrices = Bitpanda.getCurrentPrices masterData

            let! assets = Assets.getAssets masterData transactions

            return assets, currentPrices, masterData
        }

    let run (fetchResult: Result<(Asset list * Map<int, Map<string, decimal>> * MasterData), Oryx.HandlerError<obj>>) =
        taskResult {
            let! (assets, currentPrices, masterData) = fetchResult

            let assets =
                assets
                |> List.map (Wizard.confirmAssetAmount configuration)
                |> List.append (Wizard.addAdditionalCoins configuration masterData)

            configuration.WriteConfiguration()

            return assets, currentPrices, masterData
        }
        |> TaskResult.map (fun (assets, currentPrices, masterData) ->
            let table = Table()
            table.Border <- TableBorder.SimpleHeavy

            let (culture, totalPricePaid) =
                calculateTotalPricePaid masterData assets

            let totalPricePaidColumn = TableColumn("Total Price Paid")

            totalPricePaidColumn.Footer <- Text(totalPricePaid.ToString("c", culture)) :> IRenderable

            let (totalCurrentValueCulture, totalCurrentValue) =
                calculateTotalCurrentValue masterData currentPrices assets

            let totalCurrentPriceColumn = TableColumn("Total Current Value")

            totalCurrentPriceColumn.Footer <-
                Text(totalCurrentValue.ToString("c", totalCurrentValueCulture)) :> IRenderable

            let percentChangeColumn = TableColumn("Change")

            percentChangeColumn.Footer <-
                Markup($"{calculatePercentChange totalCurrentValue totalPricePaid}") :> IRenderable

            let differenceColumn = TableColumn(String.Empty)

            let difference =
                let diff = (totalCurrentValue - totalPricePaid)
                let color = if diff > 0.0M then "green" else "red"

                let culture =
                    if culture = totalCurrentValueCulture then culture else CultureInfo("de-DE")

                let diff = diff.ToString("c", culture)

                $"[{color}]{diff}[/]"

            differenceColumn.Footer <- Markup($"{difference}") :> IRenderable


            //{difference}

            table.AddColumn("Coin") |> ignore
            table.AddColumn("Amount Owned") |> ignore
            table.AddColumn(totalPricePaidColumn) |> ignore
            table.AddColumn("Current Price") |> ignore
            table.AddColumn(totalCurrentPriceColumn) |> ignore
            table.AddColumn(differenceColumn) |> ignore
            table.AddColumn(percentChangeColumn) |> ignore

            [ 1 .. 6 ]
            |> Seq.iter (fun i -> table.Columns.[i].RightAligned() |> ignore)

            table.Columns.[5].PadLeft(5) |> ignore

            assets
            |> Seq.iter (fun r ->
                let currentPrice =
                    currentPrices.[r.Cryptocoin.Id].[(fst r.PricePaid).Symbol]

                let totalCurrentValue = currentPrice * r.AmountOwned

                let culture =
                    if r.Properties
                       |> List.contains (Property.IsMultiCurrency) then
                        CultureInfo("de-DE")
                    else
                        getCulture (fst r.PricePaid)

                let difference =
                    let diff = (totalCurrentValue - (snd r.PricePaid))
                    let color = if diff > 0.0M then "green" else "red"

                    let diff = diff.ToString("c", culture)
                    $"[{color}]{diff}[/]"

                let hasNotes =
                    if r.Properties
                       |> List.contains (Property.HasExternalAmount) then
                        "*"
                    else
                        String.Empty

                let pricePaid = (snd r.PricePaid).ToString("c", culture)

                let percentChange =
                    calculatePercentChange totalCurrentValue (snd r.PricePaid)

                let totalCurrentPrice =
                    (totalCurrentValue).ToString("c", culture)

                let currentPrice = currentPrice.ToString("c", culture)

                table.AddRow
                    ([| Text($"{hasNotes} ({r.Cryptocoin.Symbol}) {r.Cryptocoin.Name}") :> IRenderable
                        Text($"{r.AmountOwned}") :> IRenderable
                        Text($"{pricePaid}") :> IRenderable
                        Text($"{currentPrice}") :> IRenderable
                        Text($"{totalCurrentPrice}") :> IRenderable
                        Markup($"{difference}") :> IRenderable
                        Markup($"{percentChange}") :> IRenderable |])
                |> ignore)

            AnsiConsole.Render(table)

            if assets
               |> Seq.exists (fun a ->
                   a.Properties
                   |> List.contains (Property.HasExternalAmount)) then
                AnsiConsole.WriteLine
                    ("Items denoted with (*) may have some or all coins held externally, and may not accurately be represented here")

            AnsiConsole.WriteLine
                ("If a mix of currencies exists in any calculations, then all values are converted to Euro (€) first, using the conversion rate as specified by BitPanda"))
        |> TaskResult.mapError (fun e -> printfn "An error has occured: %A" e)

    let apiKey =
        if String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BITPANDA")) then
            if not (configuration.GetConfiguration()).IsConfigured then
                let apiKey =
                    AnsiConsole.Ask<string>("BitPanda API Key: ")

                configuration.SetApiKey(apiKey)
                apiKey
            else
                (configuration.GetConfiguration()).ApiKey
        else
            Environment.GetEnvironmentVariable("BITPANDA")

    let fetchResult () =
        let status = AnsiConsole.Status()
        status.Spinner <- Spinner.Known.Dots2

        status
            .StartAsync("Getting BitPanda data...", (fun _ -> getData apiKey))
            .GetAwaiter()
            .GetResult()

    if argv |> Array.contains ("-h")
       || argv |> Array.contains ("--help") then
        printfn
            "Simple command line tool to view basic portfolio information about cryptocurrencies held in BitPanda, but also elsewhere."

        printfn "The information presented is only meant for a quick overview, and accuracy is not guaranteed."
        printfn "This project is not affiliated with BitPanda in any way."
        printfn ""
        printfn "Additional documentation can be found at: https://github.com/kaeedo/CryptoGains/blob/master/README.md"
        printfn ""

        printfn
            "To run, requires an API key from BitPanda with the \"Transaction\" scope defined. To create an API key, go to your profile page within BitPanda and click on API Key."

        printfn
            "This API key should be set within an Environment Variable called \"BITPANDA\". Alternatively, you will be prompted for it during first time setup, and then will be saved in plaintext."

        printfn ""
        printfn "-h | --help to view this help"
        printfn "-v | --version to view version number information, and check for latest version available online"
        printfn "-u | --update-configuration to reconfigure the app when you have previously configured it."

        0
    elif argv |> Array.contains ("-v")
         || argv |> Array.contains ("--version") then
        let version =
            Reflection
                .Assembly
                .GetEntryAssembly()
                .GetName()
                .Version.ToString(3)

        printfn $"CryptoGains version: {version}"

        let status = AnsiConsole.Status()
        status.Spinner <- Spinner.Known.Dots2

        let _ =
            status
                .Start("Getting latest release...",
                            (fun _ ->
                                GitHub.getLatestVersion ()
                                |> TaskResult.map (fun v ->
                                    printfn $"Latest available version: {v}"
                                    printfn $"Download at: https://github.com/kaeedo/CryptoGains/releases/latest")))
                .GetAwaiter()
                .GetResult()
                
        0
    elif argv |> Array.contains ("-u")
         || argv |> Array.contains ("--update-configuration") then
        configuration.ResetConfiguration()
        configuration.WriteConfiguration()
        run (fetchResult ()) |> ignore
        0
    else
        run (fetchResult ()) |> ignore
        0
