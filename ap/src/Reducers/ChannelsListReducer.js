import { channelsApi } from "../Api/channelsApi";
import { notification } from "antd";

const SET_CHANNELLS = "SET_CHANNELLS";
const SET_LOADING_CHANNELLS = "SET_LOADING_CHANNELLS";
const SET_LOADING_REWIVING = "SET_LOADING_REWIVING";

let initialChannelsListState = {
    channels: [/*{
        {
            channelId: "",
            channelName: "",
            serverID: "",
            serverName: "",
            deleteTime: Date(),
        }
    }*/],
    pagination: {
        Page: 0,
        Number: 0,
        PageCount: 0,
        NumberCount: 0
    },
    loadingChannels: false,
    loadingRewiving: false,
  }

const channelsListReducer = (state = initialChannelsListState, action) => {
    let newState = {...state};
    switch(action.type){
        case SET_CHANNELLS:
            newState.channels = action.Channels;
            newState.pagination.Page = action.Page;
            newState.pagination.Number = action.Number;
            newState.pagination.PageCount = action.PageCount;
            newState.pagination.NumberCount = action.NumberCount;
            return newState;
        case SET_LOADING_CHANNELLS:
            newState.loadingChannels = action.value;
            return newState;
        case SET_LOADING_REWIVING:
            newState.loadingRewiving = action.value;
            return newState;
        default:
            return newState;
    }
}

export function setLoadingChannelsActionCreator(value) {return { type: SET_LOADING_CHANNELLS, value }}
export function setLoadingRewivingActionCreator(value) {return { type: SET_LOADING_REWIVING, value }}

export function getChannelsListActionCreator(data){
    return {type: SET_CHANNELLS, Channels: data.channels, Page: data.page, Number: data.number, PageCount: data.pageCount, NumberCount: data.numberCount}
}

export function getChannelsListThunkCreator(queryParams, navigate) {
    return (dispatch) => {
        dispatch(setLoadingChannelsActionCreator(true));
        return channelsApi.getChannels(queryParams, navigate)
            .then(data => {
                if (!data) 
                {
                    return;
                }
                return dispatch(getChannelsListActionCreator(data));
            }).finally(() => dispatch(setLoadingChannelsActionCreator(false)));
    }
}

export function rewiveChannelThunkCreator(data, navigate) {
    return (dispatch) => {
        dispatch(setLoadingRewivingActionCreator(true));
        return channelsApi.rewiveChannel(data, navigate)
            .then(response => {
                if (response !== null) 
                {
                    notification.success({
                        message: "Успех",
                        description: "Канал восстановлен",
                        duration: 4,
                        placement: "topLeft"
                    });
                    window.location.reload();
                }
            }).finally(() => dispatch(setLoadingRewivingActionCreator(false)));
    };
}

export default channelsListReducer;