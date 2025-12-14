function validarFormularioSRI() {

    let cedula = document.getElementById("txtCedula").value.trim();
    let clave = document.getElementById("txtClave").value.trim();
    let fecha = document.getElementById("txtFecha").value;
    let estado = document.getElementById("ddlEstado").value;
    let tipo = document.getElementById("dpTipo").value;


    // Validar campos vacíos
    if (cedula === "") {
        alert("Debe ingresar la Cédula o RUC.");
        return false;
    }

    if (clave === "") {
        alert("Debe ingresar la clave de acceso.");
        return false;
    }

    if (fecha === "") {
        alert("Debe seleccionar una fecha.");
        return false;
    }

    if (estado === "") {
        alert("Debe seleccionar un estado.");
        return false;
    }

    if (tipo === "") {
        alert("Debe seleccionar un tipo de comprobante.");
        return false;
    }

    // Validar fecha no mayor a hoy
    let fechaIngresada = new Date(fecha);
    let hoy = new Date();

    hoy.setHours(0, 0, 0, 0);
    fechaIngresada.setHours(0, 0, 0, 0);

    if (fechaIngresada > hoy) {
        alert("La fecha no puede ser mayor al día actual.");
        return false;
    }

    return true; // Si todo está correcto permite el postback
}
