import { BrowserRouter } from 'react-router-dom';
import { Provider } from 'react-redux';
import { Helmet } from "react-helmet";

import Header from "./Models/header/header";
import Base from './Models/Base/Base';
import store from "./Store/store";

function App() {
  return (
    <>
      <Helmet>
          <title>Админпанель</title>
          <link rel="icon" href="/Logo.png" /> 
      </Helmet>
      <BrowserRouter basename=''>
        <Provider store={store}>
          <Header/>
          <Base/>
        </Provider>
      </BrowserRouter>
    </>
  );
}

export default App;
