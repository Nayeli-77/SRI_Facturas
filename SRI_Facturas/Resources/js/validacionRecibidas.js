function validarFormularioRecibidas() {

    let cedula = document.getElementById("txtCedula").value.trim();
    let clave = document.getElementById("txtClave").value.trim();
    let anio = document.getElementById("dpAnio").value.trim();
    let mes = document.getElementById("ddlMes").value.trim();
    let dia = document.getElementById("dpDia").value.trim();
    let tipo = document.getElementById("dpTipo").value.trim();

    if (cedula === "") {
        alert("Debe ingresar CÉDULA / RUC.");
        return false;
    }

    if (clave === "") {
        alert("Debe ingresar la CLAVE DE ACCESO.");
        return false;
    }

    if (anio === "") {
        alert("Debe ingresar el AÑO.");
        return false;
    }

    if (mes === "") {
        alert("Debe seleccionar el MES.");
        return false;
    }

    if (dia === "") {
        alert("Debe seleccionar el DÍA.");
        return false;
    }

    if (tipo === "") {
        alert("Debe seleccionar el DÍA.");
        return false;
    }
    return true; // permitir el submit
}
