namespace CryptoGains

type Cryptocoin =
    { Id: int
      Symbol: string
      Name: string }

type Currency =
    { Id: int
      Symbol: string
      Name: string }

type MasterData =
    { Currencies: Currency list
      Cryptocoins: Cryptocoin list }

type TradeType =
    | Buy
    | Sell
    | Withdrawal
    | Unsupported

type AmountPaid =
    { Currency: Currency
      Amount: decimal
      AmountEur: decimal }

type Trade =
    { Type: TradeType
      Cryptocoin: Cryptocoin
      Amount: decimal
      AmountPaid: AmountPaid }

type Property =
    | HasExternalAmount = 0
    | IsMultiCurrency = 1

type Asset =
    { Cryptocoin: Cryptocoin
      AmountOwned: decimal
      Properties: Property list
      PricePaid: Currency * decimal }
