import { adminApi } from "../Api/adminApi";
import { notification } from "antd";

const SET_LOADING_REGISTRATION = "SET_LOADING_REGISTRATION";

let initialRegistrationState = {
    loadingRegistration: false
  }

const registrationReducer = (state = initialRegistrationState, action) => {
    let newState = {...state};
    switch(action.type){
        case SET_LOADING_REGISTRATION:
            newState.loadingRegistration = action.value;
            return newState;
        default:
            return newState;
    }
}

export function setLoadingRegistrationActionCreator(value) {return { type: SET_LOADING_REGISTRATION, value }}

export function addRoleThunkCreator(data, navigate) {
    return (dispatch) => {
        dispatch(setLoadingRegistrationActionCreator(true));
        return adminApi.register(data, navigate)
            .then(response => {
                if (response !== null) 
                {
                    notification.success({
                        message: "Новый админ зарегистрирован!",
                        description: "Для завершения регистрации подтвердите нового админа в базе данных",
                        duration: 10,
                        placement: "topLeft"
                    });
                    navigate("/users");
                }
            })
            .finally(() => dispatch(setLoadingRegistrationActionCreator(false)));
    };
}

export default registrationReducer;