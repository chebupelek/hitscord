import routers from "../Router/routers";

function login(body) {
    return fetch(routers.login, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(body)
    }).then(response => {
        if(!response.ok){
            if (response.status === 400) {
                alert('Invalid arguments for filtration/pagination');
                return null;
            } else if (response.status === 500) {
                alert('Internal Server Error');
                return null;
            } else {
                alert(`HTTP error! Status: ${response.message}`);
                return null;
            }
        }
        return response.json();
    }).then(data => {
        localStorage.setItem("token", data.accessToken);
        return data;
    }).catch(error => {
        console.log(error);
        return null;
    });
}

export const loginApi = {
    login : login
}