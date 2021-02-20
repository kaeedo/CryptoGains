namespace CryptoGains.Console

open CryptoGains
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
                match AnsiConsole.Confirm($"Do you still own {amountBought} in total?") with
                | true -> amountBought
                | false ->
                    let amountOwned = AnsiConsole.Ask<decimal>($"How much {coin} do you own in total?")
                    amountOwned
                    
            { asset with AmountOwned = amountOwned }
        else asset