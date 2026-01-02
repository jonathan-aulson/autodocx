"use strict";
import * as moment from "moment";

export function convertStringToDate(
	datetime: string,
	currentFormat: string,
	returnFormat: string
): Date {
	const formatDate = moment(datetime, currentFormat).format(returnFormat);
	return new Date(formatDate);
}
