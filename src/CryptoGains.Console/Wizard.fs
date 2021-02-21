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
        if asset.Properties |> List.contains (Property.HasExternalAmount)
        then 
            let amountBought = asset.AmountOwned
            let coin = asset.Cryptocoin.Symbol
            
            AnsiConsole.WriteLine($"Some or all of {coin} has been withdrawn.")
            
            let amountOwned =
                match AnsiConsole.Confirm($"Do you still own {amountBought} {coin} in total?") with
                | true -> amountBought
                | false ->
                    let amountOwned = AnsiConsole.Ask<decimal>($"How much {coin} do you own in total?")
                    amountOwned
                    
            { asset with AmountOwned = amountOwned }
        else asset
        
    let addAdditionalCoins availableCoins =
        match AnsiConsole.Confirm("Do you own any other coins not bought with BitPanda?") with
        | false -> []
        | true ->
//            let multiSelect = MultiSelectionPrompt<string>()
//            multiSelect.Title <- "Which coins do you wish to add?"
//            multiSelect.PageSize <- 10
//            multiSelect.AddChoices(availableCoins) |> ignore
//            
//            let coins =
//                AnsiConsole.Prompt(multiSelect)
//                
//            let additionalAssets =
//                coins
//                |> List.map (fun c ->
//                    let amountOwned = AnsiConsole.Prompt<decimal>($"How many {c} do you own?")
//                    // TODO multi currency
//                    let pricePaid = AnsiConsole.Prompt<decimal>($"How much did you pay in total for {c}?")
//            
//                    { Asset.Cryptocoin =
//                      AmountOwned = amountOwned
//                      Properties = []
//                      PricePaid = Currency.Euro, pricePaid }
//                    )
//                
            []                
