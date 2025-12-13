import { loginApi } from "../Api/loginApi";
import { logoutApi } from "../Api/logout";

const LOGIN = "LOGIN";
const LOGOUT = "LOGOUT";

let initialHeaderState = {
    isAuth: localStorage.getItem('token') ? true : false
}

const headerReducer = (state = initialHeaderState, action) => {
    let newState = {...state};
    switch(action.type){
        case LOGIN:
            newState.isAuth = true;
            return newState;
        case LOGOUT:
            newState.name = "";
            newState.isAuth = false;
            return newState;
        default:
            return newState;
    }
}

export function loginHeaderActionCreator(){
    return {type: LOGIN}
}

export function logoutActionCreator(){
    return {type: LOGOUT}
}

export function loginThunkCreator(data, navigate) {
    return (dispatch) => {
        localStorage.clear();
        return loginApi.login(data)
            .then(response => {
                if(response !== null){
                    dispatch(loginHeaderActionCreator());
                    navigate("/");
                }
            })
    };
}

export function logoutThunkCreator(navigate) {
    return (dispatch) => {
        return logoutApi.logout()
            .then(response => {
                if(response !== null){
                    localStorage.clear();
                    dispatch(logoutActionCreator());
                    navigate("/login");
                }
            })
    };
}

export function logoutHeaderActionCreator(){
    return {type: LOGOUT}
}

export default headerReducer;