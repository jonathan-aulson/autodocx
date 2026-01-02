"use strict";
import * as moment from "moment";
export function convertStringToDate(datetime, currentFormat, returnFormat) {
    const formatDate = moment(datetime, currentFormat).format(returnFormat);
    return new Date(formatDate);
}
//# sourceMappingURL=datetime.js.map