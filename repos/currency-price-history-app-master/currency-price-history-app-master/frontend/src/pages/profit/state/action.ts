import { ActionCreator, Dispatch } from "redux";
import { ThunkAction } from "redux-thunk";
import axios from "axios";
import { API_URL_CURRENCY_GET } from "../../../config";
import { MaxProfitPerDayState } from "./reducer";
import { MaxProfitPerDay } from "../../../interface/Currency";

export enum MaxProfitPerDayActionTypes {
	GET_ALL = "GET_ALL"
}

export interface MaxProfitPerDayGetAllAction {
	type: MaxProfitPerDayActionTypes.GET_ALL;
	maxProfitPerDay: MaxProfitPerDay;
}

export type MaxProfitPerDayActions = MaxProfitPerDayGetAllAction;

export const getAllMaxProfitPerDay: ActionCreator<
	ThunkAction<
		Promise<any>,
		MaxProfitPerDayState,
		null,
		MaxProfitPerDayGetAllAction
	>
> = (currency: string) => {
	return async (dispatch: Dispatch) => {
		try {
			const response = await axios.get(API_URL_CURRENCY_GET, {
				params: {
					currency: currency
				}
			});
			dispatch({
				maxProfitPerDay: response.data,
				type: MaxProfitPerDayActionTypes.GET_ALL
			});
		} catch (err) {
			console.error(err);
		}
	};
};
