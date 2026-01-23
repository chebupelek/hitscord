import routers from "../Router/routers";
import { notification } from "antd";

function getServersList(queryParams, navigate) 
{
    return fetch(`${routers.serverList}?${queryParams}`, {
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
function getServerData(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function ChangeServerInfo(body, navigate)
{
    return fetch(routers.roleUpdate, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
            "Authorization": `Bearer ${localStorage.getItem("token")}`
        },
        body: JSON.stringify(body)
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
        return data || {};
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

function getServerData1(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData2(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData3(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData4(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData5(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData6(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData7(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData8(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData9(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData10(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData11(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData12(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData13(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData14(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData15(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData16(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData17(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

function getServerData18(queryParams, navigate)
{
    return fetch(routers.serverInfo+`?`+queryParams, {
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

export const serverApi = {
    getServersList : getServersList,
    getServerData: getServerData,
}