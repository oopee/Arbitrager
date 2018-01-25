import * as React from 'react';
import { RouteComponentProps } from 'react-router-dom';
import { connect } from 'react-redux';
import { ApplicationState }  from '../store';
import * as ArbitrageState from '../store/Arbitrage';
import { Form, Row, Col, Input, InputNumber, Button, Icon, Radio, Table  } from 'antd';
import { ColumnProps } from 'antd/lib/table';

type ITrade = ArbitrageState.ITrade;

// At runtime, Redux will merge together...
type ArbitrageProps =
    ArbitrageState.ArbitrageState           // ... state we've requested from the Redux store
    & typeof ArbitrageState.actionCreators  // ... plus action creators we've requested
    & RouteComponentProps<{}>;              // ... plus incoming routing parameters

const columns: ColumnProps<ITrade>[] = [
    {
        key: 'id',
        title: 'ID',
        dataIndex: 'id',
    },
    {
        key: 'date',
        title: 'Date',
        dataIndex: 'date',
        render: (text, record, index) => {
            return <span>{ record.date.toUTCString() }</span>
        },
    },
    {
        key: 'profitPercentage',
        title: 'Profit',
        dataIndex: 'profitPercentage',
        render: (text, record, index) => {
            if (record.profitPercentage) {
                let color = "";
                if (record.profitPercentage < 2) {
                    color = "rgb(0, 128, 0)";
                }
                else if (record.profitPercentage >= 2 && record.profitPercentage < 4) {
                    color = "rgb(0, 179, 0)"
                }
                else if (record.profitPercentage >= 4 && record.profitPercentage < 6) {
                    color = "rgb(0, 230, 0)";
                }
                else {
                    color = "rgb(0, 255, 0)";
                }

                return <span style={{ color:color }}>{ record.profitPercentage.toFixed(2) } %</span>
            }
        },
    },
    {
        key: 'message',
        title: 'Message',
        dataIndex: 'message',
    }
];

class TradeTable extends Table<ITrade> { }

class Arbitrage extends React.Component<ArbitrageProps, {}> {
    componentWillMount() {
        this.getArbitrageInfo();
    }

    getArbitrageInfo() {
        this.props.requestArbitrageInfo(this.props.eurAmount);
    }

    executeArbitrage() {
        this.props.requestExecuteArbitrage(this.props.eurAmount);
    }
    
    toggleAutomaticArbitrager() {
        let run = !this.props.automaticArbitragerRunning;
        this.props.requestAutomaticArbitrage(run);
    }

    setArbitrageMode(mode: string) {
        let typedMode = ArbitrageState.ArbitrageMode[mode as any] as ArbitrageState.ArbitrageMode;
        this.props.setArbitrageMode(typedMode);
    }

    public render() {
        let baseAsset = this.props.infoData.baseAsset;
        let quoteAsset = this.props.infoData.quoteAsset;

        const formItemLayout = {
            labelCol: {
                xs: { span: 24 },
                sm: { span: 4 },
            },
            wrapperCol: {
                xs: { span: 24 },
                sm: { span: 20 },
            },
        };

        return <div>
            <h1>Arbitrage</h1>

            <Form className="arbitrage-settings">
                    <Form.Item
                        label="Mode"
                        { ...formItemLayout }
                    >
                        <Radio.Group value={ this.props.mode } onChange={ (event) => this.setArbitrageMode(event.target.value) }>
                            <Radio.Button value={ ArbitrageState.ArbitrageMode.Manual }>Manual</Radio.Button>
                            <Radio.Button value={ ArbitrageState.ArbitrageMode.Automatic }>Automatic</Radio.Button>
                        </Radio.Group>
                    </Form.Item>

                    { this.props.mode == ArbitrageState.ArbitrageMode.Manual ?
                        <div>
                            <Form.Item
                                label="Amount"
                                { ...formItemLayout }
                            >
                                <InputNumber
                                    value={ this.props.eurAmount }
                                    onChange={ (value) => { this.props.setEurAmount(value as number) } }
                                />
                            </Form.Item>

                            <Form.Item
                                label="Actions"
                                { ...formItemLayout }
                            >
                                <Button type="primary" onClick={ () => { this.getArbitrageInfo() } }>Get info</Button>
                                <Button type="primary" onClick={ () => { this.executeArbitrage() } }>Execute</Button>
                            </Form.Item>
                        </div>
                    : [] }

                    { this.props.mode == ArbitrageState.ArbitrageMode.Automatic ?
                        <div>
                            <Form.Item
                                label="Actions"
                                { ...formItemLayout }
                            >
                                <Button type="primary" onClick={ () => { this.toggleAutomaticArbitrager() } }>{ this.props.automaticArbitragerRunning ? "Stop" : "Start" } automatic arbitrager</Button>
                            </Form.Item>
                        </div>
                    : [] }
            </Form>
            
            { this.props.isLoading ? <span>Loading...</span> : [] }

            <TradeTable
                columns={ columns }
                dataSource={ this.props.trades }
                expandedRowRender={ this.renderDetailedInfo }
            />

            <div className='container-fluid'>
                <div className='row'>
                    <div className='col-sm-5'>
                        <p className='title'>Basic information</p>
                        <table className='table'>
                            <tbody>
                                <tr>
                                    <td>Max negative spread ({ quoteAsset })</td>
                                    <td>{ this.props.infoData.maxNegativeSpread }</td>
                                </tr>
                                <tr>
                                    <td>Max negative spread (%)</td>
                                    <td>{ this.props.infoData.maxNegativeSpreadPercentage } %</td>
                                </tr>
                                <tr>
                                    <td>Buyer implementation</td>
                                    <td>{ this.props.infoData.buyerName }</td>
                                </tr>
                                <tr>
                                    <td>Seller implementation</td>
                                    <td>{ this.props.infoData.sellerName }</td>
                                </tr>
                                <tr>
                                    <td>Estimated average buy unit price ({ quoteAsset })</td>
                                    <td>{ this.props.infoData.estimatedAvgBuyUnitPrice }</td>
                                </tr>
                                <tr>
                                    <td>Estimated average sell unit price ({ quoteAsset })</td>
                                    <td>{ this.props.infoData.estimatedAvgSellUnitPrice }</td>
                                </tr>
                                <tr>
                                    <td>Estimated negative spread ({ quoteAsset })</td>
                                    <td>{ this.props.infoData.estimatedAvgNegativeSpread }</td>
                                </tr>
                                <tr>
                                    <td>Estimated negative spread (%)</td>
                                    <td>{ this.props.infoData.estimatedAvgNegativeSpreadPercentage } %</td>
                                </tr>
                                <tr>
                                    <td>{ baseAsset } balance sufficient</td>
                                    <td>{ this.props.infoData.isBaseBalanceSufficient ? 'true' : 'false' }</td>
                                </tr>
                                <tr>
                                    <td>{ quoteAsset } balance sufficient</td>
                                    <td>{ this.props.infoData.isQuoteBalanceSufficient ? 'true' : 'false' }</td>
                                </tr>
                                <tr>
                                    <td>Profitable</td>
                                    <td>{ this.props.infoData.isProfitable ? 'true' : 'false' }</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                    
                    <div className='col-sm-5'>
                        <p className='title'>Profit calculation</p>

                        <table className='table'>
                            <tbody>
                                <tr>
                                    <td>{ quoteAsset } spent</td>
                                    <td>{ this.props.infoData.profitCalculation.quoteCurrencySpent }</td>
                                </tr>
                                <tr>
                                    <td>{ quoteAsset } earned</td>
                                    <td>{ this.props.infoData.profitCalculation.quoteCurrencyEarned  }</td>
                                </tr>
                                <tr>
                                    <td>Profit ({ quoteAsset })</td>
                                    <td>{ this.props.infoData.profitCalculation.profit }</td>
                                </tr>
                                <tr>
                                    <td>Profit (%)</td>
                                    <td>{ this.props.infoData.profitCalculation.profitPercentage } %</td>
                                </tr>
                                <tr>
                                    <td>Profit after tax ({ quoteAsset })</td>
                                    <td>{ this.props.infoData.profitCalculation.profitAfterTax }</td>
                                </tr>
                                <tr>
                                    <td>{ baseAsset } buy count</td>
                                    <td>{ this.props.infoData.profitCalculation.baseCurrencyBuyCount }</td>
                                </tr>
                                <tr>
                                    <td>{ baseAsset } sell count</td>
                                    <td>{ this.props.infoData.profitCalculation.baseCurrencySellCount }</td>
                                </tr>
                                <tr>
                                    <td>Buy fee ({ quoteAsset })</td>
                                    <td>{ this.props.infoData.profitCalculation.buyFee }</td>
                                </tr>
                                <tr>
                                    <td>Sell fee ({ quoteAsset })</td>
                                    <td>{ this.props.infoData.profitCalculation.sellFee }</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>

                    <div className='col-sm-2'>
                        <p className='title'>Status</p>

                        <table className='table'>
                            <tbody>
                                <tr>
                                    <td>Buyer balance { quoteAsset }</td>
                                    <td>{ this.props.infoData.buyer.balance.quote }</td>
                                </tr>
                                <tr>
                                    <td>Buyer balance { baseAsset }</td>
                                    <td>{ this.props.infoData.buyer.balance.base }</td>
                                </tr>
                                <tr>
                                    <td>Seller balance { quoteAsset }</td>
                                    <td>{ this.props.infoData.seller.balance.quote }</td>
                                </tr>
                                <tr>
                                    <td>Seller balance { baseAsset }</td>
                                    <td>{ this.props.infoData.seller.balance.base }</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>

                <div className='row'>
                    <div className='col-sm-12'>
                        <p className='title'>State changes</p>

                        <table className='table'>
                            <tbody>
                                { this.props.states.map((state, index) => {
                                    return this.renderState(state, index)                                    
                                }) }
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>;
    }

    private renderDetailedInfo(trade: ITrade) {
        const lastTrade = trade.stateChanges[trade.stateChanges.length-1];
        const currentState = lastTrade.stateName;
        const profit = lastTrade.finishedResult ? lastTrade.finishedResult.profitPercentage : null;

        return <div>
            <p>
                Current state: { currentState }
            </p>
            <p>
                Profit: { profit }
            </p>
        </div>
    }

    private renderState(state: ArbitrageState.ArbitrageContext, index: number) {
        interface KVP {
            key: string;
            value: string;
        }

        let properties = [] as KVP[];

        if (state.stateName == "CheckStatus") {            
            properties.push({ key: state.baseAsset + " amount to buy", value: state.buyOrder_BaseCurrencyAmountToBuy.toString() });
            properties.push({ key: state.quoteAsset + " limit price", value: state.buyOrder_QuoteCurrencyLimitPriceToUse.toString() });
        }
        else if (state.stateName == "PlaceBuyOrder") {
            properties.push({ key: "BuyOrderId", value: state.buyOrderId });
        }
        else if (state.stateName == "GetBuyOrderInfo") {
            properties.push({ key: "Limit price", value: state.buyOrder.limitPrice.toString() });
            properties.push({ key: "Volume", value: state.buyOrder.volume.toString() });
            properties.push({ key: "Filled volume", value: state.buyOrder.filledVolume.toString() });
            properties.push({ key: "Cost (including fee)", value: state.buyOrder.costIncludingFee.toString() });
            properties.push({ key: "Fee", value: state.buyOrder.fee.toString() });
            properties.push({ key: "Open time", value: state.buyOrder.openTime.toString() });
            properties.push({ key: "Close time", value: state.buyOrder.closeTime.toString() });
            properties.push({ key: "Exchange", value: state.buyerName });
        }
        else if (state.stateName == "PlaceSellOrder") {
            properties.push({ key: "SellOrderId", value: state.sellOrderId });
        }
        else if (state.stateName == "GetSellOrderInfo") {
            properties.push({ key: "Limit price", value: state.sellOrder.limitPrice.toString() });
            properties.push({ key: "Volume", value: state.sellOrder.volume.toString() });
            properties.push({ key: "Filled volume", value: state.sellOrder.filledVolume.toString() });
            properties.push({ key: "Cost (including fee)", value: state.sellOrder.costIncludingFee.toString() });
            properties.push({ key: "Fee", value: state.sellOrder.fee.toString() });
            properties.push({ key: "Open time", value: state.sellOrder.openTime.toString() });
            properties.push({ key: "Close time", value: state.sellOrder.closeTime.toString() });
            properties.push({ key: "Exchange", value: state.sellerName });
        }
        else if (state.stateName == "CalculateFinalResult") {
            properties.push({ key: state.baseAsset + " bought", value: state.finishedResult.baseCurrencyBought.toString() });
            properties.push({ key: state.baseAsset + " sold", value: state.finishedResult.baseCurrencySold.toString() });
            properties.push({ key: state.baseAsset + " delta", value: state.finishedResult.baseCurrencyDelta.toString() });
            properties.push({ key: state.quoteAsset + " spent", value: state.finishedResult.quoteCurrencySpent.toString() });
            properties.push({ key: state.quoteAsset + " earned", value: state.finishedResult.quoteCurrencyEarned.toString() });
            properties.push({ key: state.quoteAsset + " delta", value: state.finishedResult.quoteCurrencyDelta.toString() });
            properties.push({ key: "Profit", value: state.finishedResult.profitPercentage.toString() + " %" });
        }

        if (state.error) {
            properties.push({ key: "Error", value: state.error });
        }

        return <tr key={ index }>
            <td>
                { state.stateName }
            </td>
            <td>
                { properties.map((col, index) => {
                    return <p key={ index }>
                        { col.key } : { col.value }
                    </p>
                })}
            </td>
        </tr>
    }
}

export default connect(
    (state: ApplicationState) => state.arbitrage,   // Selects which state properties are merged into the component's props
    ArbitrageState.actionCreators                   // Selects which action creators are merged into the component's props
)(Arbitrage) as typeof Arbitrage;
