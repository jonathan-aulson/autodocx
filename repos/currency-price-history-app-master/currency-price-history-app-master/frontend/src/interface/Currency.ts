export interface MaxProfitPerDay {
	currency: string;
	date: string;
	buy: Quote;
	sell: Quote;
	max_profit: number;
}

export interface Quote {
	time: string;
	price: string | number;
}
