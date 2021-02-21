namespace CryptoGains.Console

open CryptoGains
open FsToolkit.ErrorHandling

type Coin = { Name: string; Prices: Map<string, decimal> }

[<RequireQualifiedAccess>]
module Assets =
    let getAssets (masterData: MasterData) (transactions: Trade list) =
        taskResult {
            let transactions =
                transactions
                |> Seq.groupBy (fun t -> t.Cryptocoin.Id)
                |> Seq.map (fun (coinId, trades) ->
                        let isMultiCurrency =
                            (trades
                            |> Seq.distinctBy (fun t -> t.AmountPaid.Currency)
                            |> Seq.length) > 1
                            
                        let currency =
                            if isMultiCurrency
                            then masterData.Currencies |> List.find (fun c -> c.Symbol = "EUR")
                            else (trades |> Seq.head).AmountPaid.Currency
                            
                        let singleCurrencyFolder accumulator currentTrade =
                            let amountPaid = currentTrade.AmountPaid.Amount
                            match currentTrade.Type with
                                | TradeType.Buy ->
                                    (fst accumulator + currentTrade.Amount), (snd accumulator + (amountPaid))
                                | TradeType.Sell ->
                                    (fst accumulator - currentTrade.Amount), (snd accumulator - (amountPaid))
                                | TradeType.Withdrawal ->
                                    accumulator
                                | Unsupported ->
                                    accumulator
                                    
                        let multiCurrencyFolder accumulator currentTrade =
                            let amountPaid = currentTrade.AmountPaid.AmountEur
                            match currentTrade.Type with
                                | TradeType.Buy ->
                                    (fst accumulator + currentTrade.Amount), (snd accumulator + (amountPaid))
                                | TradeType.Sell ->
                                    (fst accumulator - currentTrade.Amount), (snd accumulator - (amountPaid))
                                | TradeType.Withdrawal ->
                                    accumulator
                                | Unsupported ->
                                    accumulator
                                
                        let (amountOwned, pricePaid) =
                            trades
                            |> Seq.fold (if isMultiCurrency then multiCurrencyFolder else singleCurrencyFolder) (0M, 0M)
                        
                        let properties =
                            [
                                if trades |> Seq.exists (fun t -> t.Type = Withdrawal)
                                then yield Property.HasExternalAmount
                                
                                if isMultiCurrency then yield Property.IsMultiCurrency
                            ]
                        
                        { Asset.Cryptocoin = (trades |> Seq.find (fun t -> t.Cryptocoin.Id = coinId)).Cryptocoin
                          AmountOwned = amountOwned
                          Properties = properties
                          PricePaid = currency, pricePaid }
                    )
            
            return
                transactions
                |> Seq.filter (fun t -> t.AmountOwned > 0M)
                |> Seq.toList
        }