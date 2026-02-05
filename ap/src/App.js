import { BrowserRouter } from 'react-router-dom';
import { Provider } from 'react-redux';
import { HelmetProvider } from 'react-helmet-async';
import { Helmet } from 'react-helmet-async';
import { Layout } from 'antd';
import SideMenu from './Models/header/SideMenu';
//import Header from "./Models/header/header";
import Base from './Models/Base/Base';
import store from "./Store/store";

const { Content } = Layout;

const SIDEBAR_WIDTH = 260;

function App() 
{
	return (
		<HelmetProvider>
			<Helmet>
				<title>Админпанель</title>
				<link rel="icon" href="/Logo.png" />
			</Helmet>
			<BrowserRouter>
				<Provider store={store}>
					<Layout style={{ minHeight: '100vh' }}>
						<SideMenu />
						<Layout style={{ marginLeft: SIDEBAR_WIDTH }}>
							<Content style={{ padding: '16px' }}>
								<Base />
							</Content>
						</Layout> 
					</Layout>
				</Provider>
			</BrowserRouter>
		</HelmetProvider>
	);
}

export default App;
