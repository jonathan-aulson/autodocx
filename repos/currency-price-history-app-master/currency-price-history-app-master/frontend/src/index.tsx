import * as React from "react";
import * as ReactDOM from "react-dom";
import { Provider } from "react-redux";
import { Store } from "redux";
import "./index.css";
import AppBar from "@material-ui/core/AppBar";
import CssBaseline from "@material-ui/core/CssBaseline";
import Toolbar from "@material-ui/core/Toolbar";
import Typography from "@material-ui/core/Typography";
import { withStyles, makeStyles } from "@material-ui/core/styles";
import Table from "@material-ui/core/Table";
import TableBody from "@material-ui/core/TableBody";
import TableCell from "@material-ui/core/TableCell";
import TableHead from "@material-ui/core/TableHead";
import TableRow from "@material-ui/core/TableRow";
import Paper from "@material-ui/core/Paper";
import App from "./App";
import * as serviceWorker from "./serviceWorker";
import configureStore, { AppState } from "./state/store";

interface Props {
	store: Store<AppState>;
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

const useStyles = makeStyles(theme => ({
	root: {
		width: "100%",
		marginTop: theme.spacing(3),
		overflowX: "auto"
	},
	table: {
		minWidth: 700
	},
	footer: {
		backgroundColor: theme.palette.background.paper,
		padding: theme.spacing(6)
	}
}));

export const Root: React.FC<Props> = props => {
	const classes = useStyles();

	return (
		<Provider store={props.store}>
			<CssBaseline />
			<AppBar position="relative">
				<Toolbar>
					<Typography variant="h6" color="inherit" noWrap>
						Currency Price History App by PP
					</Typography>
				</Toolbar>
			</AppBar>
			<main>
				<Paper className={classes.root}>
					<Table className={classes.table}>
						<TableHead>
							<TableRow>
								<StyledTableCell align="center">Date</StyledTableCell>
								<StyledTableCell align="center">Currency</StyledTableCell>
								<StyledTableCell align="right">Buy&nbsp;Time</StyledTableCell>
								<StyledTableCell align="right">Buy&nbsp;Price</StyledTableCell>
								<StyledTableCell align="right">Sell&nbsp;Time</StyledTableCell>
								<StyledTableCell align="right">Sell&nbsp;Price</StyledTableCell>
								<StyledTableCell align="right">Profit&nbsp;($)</StyledTableCell>
							</TableRow>
						</TableHead>
						<TableBody>
							<App />
						</TableBody>
					</Table>
				</Paper>
			</main>
			<footer className={classes.footer}>
				<Typography
					variant="subtitle1"
					align="center"
					color="textSecondary"
					component="p"
				>
					Copyright &copy; 2019 Currency App By PP
				</Typography>
			</footer>
		</Provider>
	);
};

const store = configureStore();

ReactDOM.render(<Root store={store} />, document.getElementById(
	"root"
) as HTMLElement);

serviceWorker.unregister();
