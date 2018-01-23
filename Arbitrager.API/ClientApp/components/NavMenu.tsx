import * as React from 'react';
import { Link } from 'react-router-dom';
import { Layout, Menu, Icon } from 'antd';
const { Header, Content, Footer, Sider } = Layout;

export class NavMenu extends React.Component<{}, {}> {
    public render() {
        return <Sider
            breakpoint="lg"
            collapsible={true}
        >
            <Menu theme="dark" mode="inline" defaultSelectedKeys={['1']}>
                <Menu.Item key="1">
                    <Icon type="code-o" />
                    <span className="nav-text">Trade</span>
                    <Link to={ '/' } />
                </Menu.Item>
                <Menu.Item key="2">
                    <Icon type="line-chart" />
                    <span className="nav-text">Spread history</span>
                    <Link to={ '/' } />
                </Menu.Item>
                <Menu.Item key="3">
                    <Icon type="table" />
                    <span className="nav-text">Trade history</span>
                    <Link to={ '/' } />
                </Menu.Item>
            </Menu>
        </Sider>
    }
}
