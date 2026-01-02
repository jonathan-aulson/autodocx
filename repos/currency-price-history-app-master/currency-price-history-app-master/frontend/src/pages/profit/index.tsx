import * as React from "react";
import { connect } from "react-redux";
import { AppState } from "../../state/store";
import { getAllMaxProfitPerDay } from "./state/action";
import { MaxProfitPerDay } from "../../interface/Currency";

import { withStyles } from "@material-ui/core/styles";
import TableCell from "@material-ui/core/TableCell";
import TableRow from "@material-ui/core/TableRow";

export interface Props {
	maxProfitsPerDay: {
		[index: string]: MaxProfitPerDay;
	};
	currency: string;
	getAllMaxProfitPerDay: (currency: string) => void;
}

const StyledTableCell = withStyles(theme => ({
	head: {
		backgroundColor: theme.palette.common.black,
		color: theme.palette.common.white
	},
	body: {
		fontSize: 14
	}
}))(TableCell);

const StyledTableRow = withStyles(theme => ({
	root: {
		"&:nth-of-type(odd)": {
			backgroundColor: theme.palette.background.default
		}
	}
}))(TableRow);

class MaxProfitPerDayList extends React.Component<Props & any> {
	componentDidMount() {
		this.props.getAllMaxProfitPerDay(this.props.currency);
	}

	public render() {
		const { maxProfitsPerDay, currency } = this.props;
		if (typeof maxProfitsPerDay[currency] !== "undefined") {
			return (
				<React.Fragment>
					<StyledTableRow key={currency}>
						<StyledTableCell align="center">
							{maxProfitsPerDay[currency]["date"]}
						</StyledTableCell>
						<StyledTableCell align="center">{currency}</StyledTableCell>
						<StyledTableCell align="right">
							{maxProfitsPerDay[currency]["buy"]["time"]}
						</StyledTableCell>
						<StyledTableCell align="right">
							$ {maxProfitsPerDay[currency]["buy"]["price"]}
						</StyledTableCell>
						<StyledTableCell align="right">
							{maxProfitsPerDay[currency]["sell"]["time"]}
						</StyledTableCell>
						<StyledTableCell align="right">
							$ {maxProfitsPerDay[currency]["sell"]["price"]}
						</StyledTableCell>
						<StyledTableCell align="right">
							$ {maxProfitsPerDay[currency]["max_profit"]}
						</StyledTableCell>
					</StyledTableRow>
				</React.Fragment>
			);
		} else {
			return (
				<React.Fragment>
					<StyledTableRow key={currency}>
						<StyledTableCell component="th" scope="row" colSpan={6}>
							Loading...
						</StyledTableCell>
					</StyledTableRow>
				</React.Fragment>
			);
		}
	}
}

const mapStateToProps = (store: AppState) => {
	return {
		maxProfitsPerDay: store.maxProfitPerDayState.maxProfitPerDay
	};
};
const mapDispatchToProps = (dispatch: any) => ({
	getAllMaxProfitPerDay: (currency: string) =>
		dispatch(getAllMaxProfitPerDay(currency)),
	dispatch: dispatch
});

export default connect(
	mapStateToProps,
	mapDispatchToProps
)(MaxProfitPerDayList);
