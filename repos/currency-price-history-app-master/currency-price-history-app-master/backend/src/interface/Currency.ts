export interface CurrencyData {
	currency: string;
	date: string;
	quotes: Array<Quote[]>;
}

export interface Quote {
	time: string;
	price: string|number;
}
