namespace CryptoGains

type TradeType =
    | Buy = 0
    | Sell = 1
    | Swap = 2

type Cryptocoin =
    { Id: int
      Symbol: string
      Name: string }

type Trade =
    { Type: TradeType
      //Cryptocoin: Cryptocoin
      Amount: decimal
      Price: decimal }
