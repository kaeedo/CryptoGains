namespace CryptoGains

type TradeType =
    | Buy = 0
    | Sell = 1

type Cryptocoin =
    { BitpandaId: int
      CoinGeckoId: string
      Symbol: string
      Name: string }

type Trade =
    { Type: TradeType
      Cryptocoin: Cryptocoin
      Amount: decimal
      Price: decimal }

type Asset =
    { Cryptocoin: Cryptocoin
      AmountOwned: decimal
      PricePaid: decimal }