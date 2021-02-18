namespace CryptoGains

type TradeType =
    | Buy
    | Sell
    | Withdrawal

//type Cryptocoin =
//    { BitpandaId: int
//      CoinGeckoId: string
//      Symbol: string
//      Name: string }

type Cryptocoin =
    { Id: int
      Symbol: string }

type Trade =
    { Type: TradeType
      Cryptocoin: Cryptocoin
      Amount: decimal
      AmountPaid: decimal }

type Asset =
    { Cryptocoin: Cryptocoin
      AmountOwned: decimal
      PricePaid: decimal }