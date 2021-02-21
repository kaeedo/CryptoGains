namespace CryptoGains.Console

open CryptoGains
open FsToolkit.ErrorHandling
open Spectre.Console
open Spectre.Console
open Spectre.Console
open Spectre.Console

[<RequireQualifiedAccess>]
module Wizard =
    let confirmAssetAmount (asset: Asset) =
        if asset.Properties
           |> List.contains (Property.HasExternalAmount) then
            let amountBought = asset.AmountOwned
            let coin = asset.Cryptocoin.Symbol

            AnsiConsole.WriteLine($"Some or all of {coin} has been withdrawn.")

            let amountOwned =
                match AnsiConsole.Confirm($"Do you still own {amountBought} {coin} in total?") with
                | true -> amountBought
                | false ->
                    let amountOwned =
                        AnsiConsole.Ask<decimal>($"How much {coin} do you own in total?")

                    amountOwned

            { asset with AmountOwned = amountOwned }
        else
            asset

    let addAdditionalCoins (masterData: MasterData) =
        match AnsiConsole.Confirm("Do you own any other coins not bought with BitPanda?") with
        | false -> []
        | true ->
            let availableCoins =
                masterData.Cryptocoins
                |> List.map (fun c -> $"({c.Symbol}) {c.Name}")

            let multiSelect = MultiSelectionPrompt<string>()
            multiSelect.Title <- "Which coins do you wish to add?"
            multiSelect.PageSize <- 10
            multiSelect.AddChoices(availableCoins) |> ignore

            let coins = AnsiConsole.Prompt(multiSelect)

            let additionalAssets =
                coins
                |> Seq.map (fun c ->
                    let amountOwned = AnsiConsole.Ask<decimal>($"How many {c} do you own?")
                    // TODO multi currency
                    let pricePaid = AnsiConsole.Ask<decimal>($"How much did you pay in total for {c}?")

                    let cryptocoin =
                        masterData.Cryptocoins
                        |> List.find (fun coin ->
                            let promptText = $"({coin.Symbol}) {coin.Name}"
                            promptText = c)

                    { Asset.Cryptocoin = cryptocoin
                      AmountOwned = amountOwned
                      Properties = [ Property.HasExternalAmount ]
                      PricePaid =
                          { Currency.Id = 1
                            Symbol = "EUR"
                            Name = "Euro" },
                          pricePaid })
                |> Seq.toList

            additionalAssets
