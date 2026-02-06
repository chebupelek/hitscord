import routers from "../Router/routers";
import { notification } from "antd";

function getOperations(queryParams, navigate) 
{
    return fetch(`${routers.operations}?${queryParams}`, {
        method: "GET",
        headers: {
            "Content-Type": "application/json",
            "Authorization": `Bearer ${localStorage.getItem("token")}`
        }
    })
    .then(async response => {
        const text = await response.text();
        let data;
        try 
        {
            data = text ? JSON.parse(text) : null;
        } 
        catch (e) 
        {
            data = text;
        }
        if (!response.ok) 
        {
            switch (response.status) 
            {
                case 400:
                    notification.error(
                        {
                            message: "Проблемы с пагинацией",
                            description: typeof data === 'object' ? data.message || JSON.stringify(data) : data,
                            duration: 4,
                            placement: "topLeft"
                        }
                    );
                    return null;
                case 404:
                    notification.error(
                        {
                            message: "Объект не найден",
                            description: typeof data === 'object' ? data.message || JSON.stringify(data) : data,
                            duration: 4,
                            placement: "topLeft"
                        }
                    );
                    return null;
                case 401:
                    notification.error(
                        {
                            message: "Ошибка с аутентификацией",
                            description: "Не пройдено",
                            duration: 4,
                            placement: "topLeft"
                        }
                    );
                    localStorage.clear();
                    navigate("/login");
                    return null;
                case 500:
                    notification.error(
                        {
                            message: "Проблема в работе сервера",
                            description: typeof data === 'object' ? data.message || JSON.stringify(data) : data,
                            duration: 4,
                            placement: "topLeft"
                        }
                    );
                    return null;
                default:
                    notification.error(
                        {
                            message: `Ошибка HTTP ${response.status}`,
                            description: typeof data === 'object' ? data.message || JSON.stringify(data) : data,
                            duration: 4,
                            placement: "topLeft"
                        }
                    );
                    return null;
            }
        }
        return data;
    })
    .catch(error => {
        console.error(error.Message);
        notification.error(
            {
                message: "Ошибка сети",
                description: error.Message ,
                duration: 4,
                placement: "topLeft"
            }
        );
        return null;
    });
}

export const operationsApi = {
    getOperations : getOperations
}