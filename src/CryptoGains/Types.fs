namespace CryptoGains

type TradeType =
    | Buy
    | Sell
    | Withdrawal
    | Unsupported

type Currency =
    | Euro = 1
    | UsDollar = 2
    | SwissFranc = 3
    | BritishPounds = 4
    | TurkishLira = 5

type Cryptocoin = { Id: int; Symbol: string }

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
