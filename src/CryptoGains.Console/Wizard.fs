namespace CryptoGains.Console

open CryptoGains
open CryptoGains.Console.Configuration
open FsToolkit.ErrorHandling
open Spectre.Console

[<RequireQualifiedAccess>]
module Wizard =
    let confirmAssetAmount (configuration: Configuration) (asset: Asset) =
        let config = configuration.GetConfiguration()

        if asset.Properties
           |> List.contains (Property.HasExternalAmount)
           && not config.IsConfigured then
            let amountBought = asset.AmountOwned
            let coin = asset.Cryptocoin.Symbol

            AnsiConsole.WriteLine($"Some or all of {coin} has been withdrawn.")

            let amountOwned =
                match AnsiConsole.Confirm($"Do you still own {amountBought} {coin} in total?") with
                | true ->
                    let withdrawnCryptocoin =
                        { WithdrawnCryptocoin.Cryptocoin = asset.Cryptocoin
                          AmountHeld = amountBought }

                    configuration.SetWithdrawnCryptocoins(withdrawnCryptocoin :: config.WithdrawnCryptocoins)
                    amountBought
                | false ->
                    let amountOwned =
                        AnsiConsole.Ask<decimal>($"How much {coin} do you own in total?")

                    amountOwned

            let withdrawnCryptocoin =
                { WithdrawnCryptocoin.Cryptocoin = asset.Cryptocoin
                  AmountHeld = amountOwned }

            configuration.SetWithdrawnCryptocoins(withdrawnCryptocoin :: config.WithdrawnCryptocoins)
            { asset with AmountOwned = amountOwned }
        elif asset.Properties
             |> List.contains (Property.HasExternalAmount)
             && config.IsConfigured then
            let coin = asset.Cryptocoin.Symbol

            let configuredCoin =
                config.WithdrawnCryptocoins
                |> List.tryFind (fun wc -> wc.Cryptocoin.Symbol = coin)

            match configuredCoin with
            | None -> asset
            | Some cc ->
                let amountOwned = cc.AmountHeld
                { asset with AmountOwned = amountOwned }
        else
            asset

    let addAdditionalCoins (configuration: Configuration) (masterData: MasterData) =
        let config = configuration.GetConfiguration()

        if config.IsConfigured then
            config.ExternallyHeld
            |> List.map (fun eh ->
                { Asset.Cryptocoin = eh.Cryptocoin
                  AmountOwned = eh.AmountHeld
                  Properties = [ Property.HasExternalAmount ]
                  PricePaid = eh.PricePaid })
        else
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
                        let amountOwned =
                            AnsiConsole.Ask<decimal>($"How many {c} do you own?")

                        let pricePaid =
                            AnsiConsole.Ask<decimal>($"How much did you pay in total for {c}?")

                        let selectionPrompt = SelectionPrompt<string>()
                        selectionPrompt.Title <- $"With which currency did you buy {c}?"
                        selectionPrompt.PageSize <- masterData.Currencies |> List.length

                        selectionPrompt.AddChoices
                            (masterData.Currencies
                             |> List.map (fun c -> c.Name))
                        |> ignore

                        let currency = AnsiConsole.Prompt(selectionPrompt)

                        let cryptocoin =
                            masterData.Cryptocoins
                            |> List.find (fun coin ->
                                let promptText = $"({coin.Symbol}) {coin.Name}"
                                promptText = c)

                        { Asset.Cryptocoin = cryptocoin
                          AmountOwned = amountOwned
                          Properties = [ Property.HasExternalAmount ]
                          PricePaid =
                              masterData.Currencies
                              |> List.find (fun c -> c.Name = currency),
                              pricePaid })
                    |> Seq.toList

                let externallyHeldCryptocoins =
                    additionalAssets
                    |> List.map (fun aa ->
                        { ExternallyHeldCryptocoin.Cryptocoin = aa.Cryptocoin
                          AmountHeld = aa.AmountOwned
                          PricePaid = aa.PricePaid })

                configuration.SetExternallyHeld(externallyHeldCryptocoins)

                additionalAssets
