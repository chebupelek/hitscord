import routers from "../Router/routers";
import { notification } from "antd";

function logout(navigate) 
{
    return fetch(routers.logout, {
        method: "DELETE",
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
                            message: "Проблемы с входными данными",
                            description: typeof data === 'object' ? data.MessageFront || JSON.stringify(data) : data,
                            duration: 4,
                            placement: "topLeft"
                        }
                    );
                    return null;
                case 404:
                    notification.error(
                        {
                            message: "Объект не найден",
                            description: typeof data === 'object' ? data.MessageFront || JSON.stringify(data) : data,
                            duration: 4,
                            placement: "topLeft"
                        }
                    );
                    return null;
                case 401:
                    notification.error(
                        {
                            message: "Ошибка с аутентификацией",
                            description: typeof data === 'object' ? data.MessageFront || JSON.stringify(data) : data,
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
                            description: typeof data === 'object' ? data.MessageFront || JSON.stringify(data) : data,
                            duration: 4,
                            placement: "topLeft"
                        }
                    );
                    return null;
                default:
                    notification.error(
                        {
                            message: `Ошибка HTTP ${response.status}`,
                            description: typeof data === 'object' ? data.MessageFront || JSON.stringify(data) : data,
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
        console.error(error.message);
        notification.error(
            {
                message: "Ошибка сети",
                description: error.message,
                duration: 4,
                placement: "topLeft"
            }
        );
        return null;
    });
}

export const logoutApi = {
    logout : logout
}