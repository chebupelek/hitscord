import { HashRouter } from 'react-router-dom';
import { Provider } from 'react-redux';

import Header from "./Models/header/header";
import Base from './Models/Base/Base';
import store from "./Store/store";

function App() {
  return (
    <HashRouter basename=''>
      <Provider store={store}>
        <Header/>
        <Base/>
      </Provider>
    </HashRouter>
  );
}

export default App;
