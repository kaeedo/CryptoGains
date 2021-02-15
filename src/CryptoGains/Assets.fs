namespace CryptoGains

open FsToolkit.ErrorHandling

type Coin = { Name: string; Prices: Map<string, decimal> }

[<RequireQualifiedAccess>]
module Assets =
    let getAssets (transactions: Trade list) =
        taskResult {
            let transactions =
                transactions
                |> Seq.groupBy (fun t -> t.Cryptocoin.BitpandaId)
                |> Seq.map (fun (coinId, trades) ->
                        let (amountOwned, pricePaid) =
                            trades
                            |> Seq.fold (fun accumulator currentTrade ->
                                match currentTrade.Type with
                                | TradeType.Buy ->
                                    (fst accumulator + currentTrade.Amount), (snd accumulator + (currentTrade.Amount * currentTrade.Price))
                                | TradeType.Sell ->
                                    (fst accumulator - currentTrade.Amount), (snd accumulator - (currentTrade.Amount * currentTrade.Price))
                                ) (0M, 0M)
                        
                        
                        { Asset.Cryptocoin = (trades |> Seq.find (fun t -> t.Cryptocoin.BitpandaId = coinId)).Cryptocoin
                          AmountOwned = amountOwned
                          PricePaid = pricePaid }
                    )
            
            return
                transactions
                |> Seq.filter (fun t -> t.AmountOwned > 0M)
                |> Seq.toList
        }