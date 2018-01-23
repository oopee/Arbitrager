import * as React from 'react';
import { Layout, Menu, Icon } from 'antd';
const { Header, Content, Footer, Sider } = Layout;
import { NavMenu } from './NavMenu';

export class MainLayout extends React.Component<{}, {}> {
    public render() {
        return <Layout style={{ height:"100vh" }}>
            <NavMenu/>
            <Layout>
                <Content style={{ margin: '24px 16px 0' }}>
                    <div style={{ padding: 24, background: '#fff', minHeight:"calc(100vh - 2*24px)" }}>
                        { this.props.children }
                    </div>
                </Content>
            </Layout>
        </Layout>
    }
}
