using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Image;
using iText.IO.Font;
using iText.Kernel.Font;

namespace OXXO_Sistema
{
    public partial class Form1 : Form
    {
        private static string cadenaConexion = "Server=LAPTOP-1LEORHM7\\SQLEXPRESS;Database=OXXO;Integrated Security=true;TrustServerCertificate=true;";
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // Desactivar todas las pestañas al iniciar
            foreach (TabPage tab in tabControl1.TabPages)
            {
                tab.Enabled = false;
            }
        }
        private void ConfigurarPermisos(string cargo)
        {
            if (cargo == "Gerente")
            {
                // Activar todas las pestañas
                foreach (TabPage tab in tabControl1.TabPages)
                {
                    tab.Enabled = true;
                }
            }
            else if (cargo == "Empleado")
            {
                // Desactivar todas las pestañas
                foreach (TabPage tab in tabControl1.TabPages)
                {
                    tab.Enabled = false;
                }

                // Activar solo la pestana de venta
                tabControl1.TabPages["tabPage1"].Enabled = true;
            }
            else
            {
                MessageBox.Show("Cargo desconocido. Verifica la configuración de la base de datos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void btnIngresar_Click(object sender, EventArgs e)
        {
            string empleadoID = txtUsuario.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(empleadoID) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Por favor, ingresa un usuario y contraseña.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("Login", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Agregar parámetros
                        command.Parameters.AddWithValue("@EmpleadoID", int.Parse(empleadoID));
                        command.Parameters.AddWithValue("@Password", password);

                        // Ejecutar el procedimiento almacenado
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string cargo = reader["Cargo"].ToString();
                                string nombre = reader["Nombre"].ToString();
                                string apellido = reader["Apellido"].ToString();

                                // Cambiar el texto de las etiquetas
                                lblPuesto.Text = cargo;
                                lblNombre.Text = $"{nombre} {apellido}";

                                // Bloquear los campos de usuario y contraseña
                                txtUsuario.ReadOnly = true;
                                txtPassword.ReadOnly = true;

                                // Mostrar mensaje de bienvenida y configurar permisos
                                MessageBox.Show($"Bienvenido {nombre} {apellido}. Cargo: {cargo}", "Inicio de sesión exitoso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                ConfigurarPermisos(cargo);
                            }
                            else
                            {
                                MessageBox.Show("Credenciales incorrectas o empleado no vigente.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                txtUsuario.ReadOnly = false;
                                txtPassword.ReadOnly = false;
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error de base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnEmpleadosSucursal_Click(object sender, EventArgs e)
        {
            dgvEmpleados.DataSource = null;
            // Limpiar el DataGridView antes de cargar los nuevos datos
            dgvEmpleados.Rows.Clear();
            dgvEmpleados.Refresh();

            // Obtener el EmpleadoID del usuario actual desde el txtUsuario
            int empleadoID = int.Parse(txtUsuario.Text.Trim()); // Asumiendo que el ID es un número

            // Consulta para llamar al procedimiento almacenado
            string query = "EXEC ObtenerEmpleadosPorSucursal @EmpleadoID";

            try
            {
                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    // Crear un SqlDataAdapter para ejecutar la consulta y llenar el DataTable
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    adapter.SelectCommand.Parameters.AddWithValue("@EmpleadoID", empleadoID); // Agregar el parámetro

                    DataTable empleadosTable = new DataTable();
                    adapter.Fill(empleadosTable); // Rellenar el DataTable con los resultados

                    dgvEmpleados.DataSource = empleadosTable; // Asignar el DataTable al DataGridView
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar empleados: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnTodosEmpleados_Click(object sender, EventArgs e)
        {
            try
            {
                // Limpia el DataGridView antes de asignar nuevos datos
                dgvEmpleados.DataSource = null;
                dgvEmpleados.Rows.Clear();
                dgvEmpleados.Refresh();

                // Consulta para obtener todos los empleados
                string query = "EXEC ConsultarEmpleados";

                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    // Crear un adaptador y llenar el DataTable
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable empleadosTable = new DataTable();
                    adapter.Fill(empleadosTable);

                    // Asignar el DataTable como fuente de datos
                    dgvEmpleados.DataSource = empleadosTable;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar empleados: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnBuscarEmpleado_Click(object sender, EventArgs e)
        {
            // Limpiar el DataGridView antes de mostrar el nuevo dato
            dgvEmpleados.DataSource = null;
            dgvEmpleados.Rows.Clear();
            dgvEmpleados.Refresh();

            string empleadoIDText = txtEmpleadoID.Text.Trim(); // Obtener el ID del TextBox

            if (string.IsNullOrEmpty(empleadoIDText))
            {
                MessageBox.Show("Por favor ingresa un ID de empleado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int empleadoID;
            if (!int.TryParse(empleadoIDText, out empleadoID))
            {
                MessageBox.Show("El ID de empleado debe ser un número válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "EXEC BuscarEmpleadoPorID @EmpleadoID";

            try
            {
                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    adapter.SelectCommand.Parameters.AddWithValue("@EmpleadoID", empleadoID);

                    DataTable empleadoTable = new DataTable();
                    adapter.Fill(empleadoTable); // Rellenar el DataTable con los resultados

                    if (empleadoTable.Rows.Count > 0)
                    {
                        // Mostrar los resultados en el DataGridView
                        dgvEmpleados.DataSource = empleadoTable;
                    }
                    else
                    {
                        MessageBox.Show("No se encontró un empleado con ese ID.", "Resultado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar el empleado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnAgregarEmpleado_Click(object sender, EventArgs e)
        {
            // Obtener los datos del formulario
            string nombre = txtNombreEmpleado.Text.Trim();
            string apellido = txtApellidoEmpleado.Text.Trim();
            DateTime fechaNacimiento = dtpFechaNacimientoEmpleado.Value;
            DateTime fechaIngreso = DateTime.Now.Date;
            string cargo = cbCargo.SelectedItem.ToString();  
            int sucursalID = int.Parse(txtSucursalEmpleado.Text.ToString());
            string password = txtContrasenaEmpleado.Text.Trim();
            bool vigente = true;

            // Verificar que todos los campos están completos
            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(apellido) || string.IsNullOrEmpty(cargo) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Por favor, ingresa todos los datos del empleado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "EXEC InsertarEmpleado @Nombre, @Apellido, @FechaNacimiento, @FechaIngreso, @Cargo, @SucursalID, @Password, @Vigente";

            try
            {
                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Agregar parámetros al comando
                        command.Parameters.AddWithValue("@Nombre", nombre);
                        command.Parameters.AddWithValue("@Apellido", apellido);
                        command.Parameters.AddWithValue("@FechaNacimiento", fechaNacimiento);
                        command.Parameters.AddWithValue("@FechaIngreso", fechaIngreso);
                        command.Parameters.AddWithValue("@Cargo", cargo);
                        command.Parameters.AddWithValue("@SucursalID", sucursalID);
                        command.Parameters.AddWithValue("@Password", password);
                        command.Parameters.AddWithValue("@Vigente", vigente);

                        // Ejecutar el procedimiento almacenado
                        command.ExecuteNonQuery();
                        // Limpiar los campos después de agregar el empleado
                        txtNombreEmpleado.Clear();
                        txtApellidoEmpleado.Clear();
                        dtpFechaNacimientoEmpleado.Value = DateTime.Now.Date; // Establecer la fecha de nacimiento a la fecha actual
                        cbCargo.SelectedIndex = -1;
                        txtSucursalEmpleado.Clear();
                        txtContrasenaEmpleado.Clear();

                        MessageBox.Show("Empleado agregado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar el empleado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void tabPage1_Click(object sender, EventArgs e)
        {

        }
        private void btnDespido_Click(object sender, EventArgs e)
        {
            // Obtener el ID del empleado desde el TextBox
            string empleadoID = txtEmpleadoID.Text.Trim();

            if (string.IsNullOrEmpty(empleadoID))
            {
                MessageBox.Show("Por favor, ingresa el ID del empleado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Conexión a la base de datos
                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    // Ejecutar el procedimiento almacenado DespedirEmpleado
                    using (SqlCommand command = new SqlCommand("DespedirEmpleado", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Agregar el parámetro EmpleadoID
                        command.Parameters.AddWithValue("@EmpleadoID", int.Parse(empleadoID));

                        // Ejecutar el procedimiento
                        command.ExecuteNonQuery();
                        txtEmpleadoID.Clear();
                        MessageBox.Show("Empleado despedido exitosamente.", "Operación Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error de base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnBuscarSucursal_Click(object sender, EventArgs e)
        {
            // Obtener el ID de la sucursal desde el TextBox
            string sucursalID = txtSucursalID.Text.Trim();

            if (string.IsNullOrEmpty(sucursalID))
            {
                MessageBox.Show("Por favor, ingresa el ID de la sucursal.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Conexión a la base de datos
                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    // Configurar el comando para llamar al procedimiento almacenado
                    using (SqlCommand command = new SqlCommand("BuscarSucursalPorID", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Agregar parámetro
                        command.Parameters.AddWithValue("@SucursalID", int.Parse(sucursalID));

                        // Ejecutar y llenar el DataTable
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        DataTable sucursalTable = new DataTable();
                        adapter.Fill(sucursalTable);

                        // Mostrar los resultados
                        if (sucursalTable.Rows.Count > 0)
                        {
                            dgvSucursales.DataSource = sucursalTable; // Asigna los datos al DataGridView
                        }
                        else
                        {
                            MessageBox.Show("No se encontró la sucursal con el ID proporcionado.", "Resultado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            dgvSucursales.DataSource = null; // Limpia el DataGridView si no hay resultados
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error de base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnMostrarSucursales_Click(object sender, EventArgs e)
        {
            try
            {
                // Conexión a la base de datos
                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    // Configurar el comando para llamar al procedimiento almacenado
                    using (SqlCommand command = new SqlCommand("MostrarTodasSucursales", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Ejecutar y llenar el DataTable
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        DataTable sucursalesTable = new DataTable();
                        adapter.Fill(sucursalesTable);

                        // Mostrar los resultados en el DataGridView
                        dgvSucursales.DataSource = sucursalesTable;
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error de base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnSucursalPropia_Click(object sender, EventArgs e)
        {
            try
            {
                dgvSucursales.DataSource = null;
                dgvSucursales.Rows.Clear();
                dgvSucursales.Refresh();

                // Obtener el EmpleadoID desde el txtUsuario
                int empleadoID = int.Parse(txtUsuario.Text.Trim());

                // Consulta que llama al procedimiento almacenado
                string query = "EXEC ObtenerSucursalPorEmpleado @EmpleadoID";

                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    adapter.SelectCommand.Parameters.AddWithValue("@EmpleadoID", empleadoID);

                    DataTable sucursalTable = new DataTable();
                    adapter.Fill(sucursalTable);

                    // Asignar los datos al DataGridView
                    dgvSucursales.DataSource = sucursalTable;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar la sucursal: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnMostrarProveedores_Click(object sender, EventArgs e)
        {
            try
            {
                dgvProveedores.DataSource = null;
                dgvProveedores.Rows.Clear();
                dgvProveedores.Refresh();

                // Consulta que llama al procedimiento almacenado
                string query = "EXEC ConsultarProveedores";

                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable proveedoresTable = new DataTable();
                    adapter.Fill(proveedoresTable);

                    // Asignar los datos al DataGridView
                    dgvProveedores.DataSource = proveedoresTable;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar proveedores: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnBuscarProveedor_Click(object sender, EventArgs e)
        {
            try
            {
                // Validar que se haya ingresado un ID
                if (string.IsNullOrWhiteSpace(txtProveedorID.Text))
                {
                    MessageBox.Show("Por favor, ingresa un ID de proveedor.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int proveedorID;
                if (!int.TryParse(txtProveedorID.Text.Trim(), out proveedorID))
                {
                    MessageBox.Show("El ID de proveedor debe ser un número.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                dgvProveedores.DataSource = null;
                dgvProveedores.Rows.Clear();
                dgvProveedores.Refresh();

                // Consulta que llama al procedimiento almacenado
                string query = "EXEC BuscarProveedorPorID @ProveedorID";

                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    adapter.SelectCommand.Parameters.AddWithValue("@ProveedorID", proveedorID);

                    DataTable proveedorTable = new DataTable();
                    adapter.Fill(proveedorTable);

                    // Verificar si se encontraron datos
                    if (proveedorTable.Rows.Count > 0)
                    {
                        dgvProveedores.DataSource = proveedorTable;
                    }
                    else
                    {
                        MessageBox.Show("No se encontró un proveedor con el ID especificado.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar el proveedor: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnAgregarProveedor_Click(object sender, EventArgs e)
        {
            try
            {
                // Validar campos
                if (string.IsNullOrWhiteSpace(txtNombreProveedor.Text) ||
                    string.IsNullOrWhiteSpace(txtTelefonoProveedor.Text) ||
                    string.IsNullOrWhiteSpace(txtEmailProveedor.Text) ||
                    string.IsNullOrWhiteSpace(txtDireccionProveedor.Text))
                {
                    MessageBox.Show("Por favor, llena todos los campos.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string nombre = txtNombreProveedor.Text.Trim();
                string telefono = txtTelefonoProveedor.Text.Trim();
                string email = txtEmailProveedor.Text.Trim();
                string direccion = txtDireccionProveedor.Text.Trim();

                // Conexión a la base de datos
                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("AgregarProveedor", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Agregar parámetros
                        command.Parameters.AddWithValue("@Nombre", nombre);
                        command.Parameters.AddWithValue("@Telefono", telefono);
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@Direccion", direccion);

                        // Ejecutar el comando
                        command.ExecuteNonQuery();

                        MessageBox.Show("Proveedor agregado con éxito.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Limpiar campos después de agregar
                        txtNombreProveedor.Clear();
                        txtTelefonoProveedor.Clear();
                        txtEmailProveedor.Clear();
                        txtDireccionProveedor.Clear();
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error en la base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnMisProductos_Click(object sender, EventArgs e)
        {
            try
            {
                // Validar que el usuario haya iniciado sesión
                if (string.IsNullOrWhiteSpace(txtUsuario.Text))
                {
                    MessageBox.Show("Debes iniciar sesión primero.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int empleadoID = int.Parse(txtUsuario.Text.Trim()); // ID del empleado actual

                // Consulta al procedimiento almacenado
                string query = "EXEC ObtenerMisProductos @EmpleadoID";

                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@EmpleadoID", empleadoID);

                        DataTable productosTable = new DataTable();
                        adapter.Fill(productosTable); // Llenar la tabla con los datos obtenidos

                        // Mostrar los datos en el DataGridView
                        dgvProductos.DataSource = productosTable;
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error en la base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnMisProveedores_Click(object sender, EventArgs e)
        {
            try
            {
                // Validar que el usuario haya iniciado sesión
                if (string.IsNullOrWhiteSpace(txtUsuario.Text))
                {
                    MessageBox.Show("Debes iniciar sesión primero.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int empleadoID = int.Parse(txtUsuario.Text.Trim()); // ID del empleado actual

                // Consulta al procedimiento almacenado
                string query = "EXEC ObtenerMisProveedores @EmpleadoID";

                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@EmpleadoID", empleadoID);

                        DataTable proveedoresTable = new DataTable();
                        adapter.Fill(proveedoresTable); // Llenar la tabla con los datos obtenidos

                        // Mostrar los datos en el DataGridView
                        dgvProveedores.DataSource = proveedoresTable;
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error en la base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnEliminarProveedor_Click(object sender, EventArgs e)
        {
            try
            {
                // Validar que se haya ingresado un ID de proveedor
                if (string.IsNullOrWhiteSpace(txtProveedorID.Text))
                {
                    MessageBox.Show("Por favor, ingresa el ID del proveedor a eliminar.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int proveedorID = int.Parse(txtProveedorID.Text.Trim()); // Obtener el ID del proveedor

                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    // Desactivar el proveedor
                    using (SqlCommand cmdDesactivarProveedor = new SqlCommand("DesactivarProveedor", connection))
                    {
                        cmdDesactivarProveedor.CommandType = CommandType.StoredProcedure;
                        cmdDesactivarProveedor.Parameters.AddWithValue("@ProveedorID", proveedorID);
                        cmdDesactivarProveedor.ExecuteNonQuery();
                    }

                    // Desactivar los productos del proveedor
                    using (SqlCommand cmdDesactivarProductos = new SqlCommand("DesactivarProductosDelProveedor", connection))
                    {
                        cmdDesactivarProductos.CommandType = CommandType.StoredProcedure;
                        cmdDesactivarProductos.Parameters.AddWithValue("@ProveedorID", proveedorID);
                        cmdDesactivarProductos.ExecuteNonQuery();
                    }
                }

                // Mostrar mensaje de éxito
                MessageBox.Show("Proveedor y sus productos marcados como no vigentes correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error en la base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnBuscarProducto_Click(object sender, EventArgs e)
        {
            // Validar que se haya ingresado un ID de producto
            if (string.IsNullOrWhiteSpace(txtProductoID.Text))
            {
                MessageBox.Show("Por favor, ingresa el ID del producto a buscar.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int productoID;

            // Verificar si el ID ingresado es válido
            if (!int.TryParse(txtProductoID.Text.Trim(), out productoID))
            {
                MessageBox.Show("El ID del producto debe ser un número válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("BuscarProducto", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Agregar el parámetro del ProductoID
                        command.Parameters.AddWithValue("@ProductoID", productoID);

                        // Ejecutar el procedimiento almacenado
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        DataTable productoTable = new DataTable();
                        adapter.Fill(productoTable);

                        if (productoTable.Rows.Count > 0)
                        {
                            // Mostrar los datos en el DataGridView
                            dgvProductos.DataSource = productoTable;
                        }
                        else
                        {
                            MessageBox.Show("No se encontró un producto con el ID proporcionado.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            dgvProductos.DataSource = null; // Limpiar el DataGridView si no se encontró nada
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error en la base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnAgregarProducto_Click(object sender, EventArgs e)
        {
            // Validar que los campos obligatorios no estén vacíos
            if (string.IsNullOrWhiteSpace(txtNombreProducto.Text) ||
                string.IsNullOrWhiteSpace(txtCategoriaProducto.Text) ||
                string.IsNullOrWhiteSpace(txtPrecioProducto.Text) ||
                string.IsNullOrWhiteSpace(txtProveedorIDProducto.Text) ||
                string.IsNullOrWhiteSpace(txtCantidadDisponible.Text))
            {
                MessageBox.Show("Por favor, completa todos los campos obligatorios.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Variables para los datos del producto
            string nombre = txtNombreProducto.Text.Trim();
            string categoria = txtCategoriaProducto.Text.Trim();
            decimal precio;
            int proveedorID, cantidadDisponible;
            int empleadoID = int.Parse(txtUsuario.Text.Trim());  // Obtener el EmpleadoID del usuario actual

            // Validar que el precio sea un número válido
            if (!decimal.TryParse(txtPrecioProducto.Text.Trim(), out precio) || precio <= 0)
            {
                MessageBox.Show("Por favor, ingresa un precio válido mayor a 0.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Validar que el ProveedorID y la CantidadDisponible sean números válidos
            if (!int.TryParse(txtProveedorIDProducto.Text.Trim(), out proveedorID) || proveedorID <= 0)
            {
                MessageBox.Show("Por favor, ingresa un ID de proveedor válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(txtCantidadDisponible.Text.Trim(), out cantidadDisponible) || cantidadDisponible <= 0)
            {
                MessageBox.Show("Por favor, ingresa una cantidad disponible válida mayor a 0.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("AgregarProducto", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Agregar parámetros al comando
                        command.Parameters.AddWithValue("@Nombre", nombre);
                        command.Parameters.AddWithValue("@Categoria", categoria);
                        command.Parameters.AddWithValue("@Precio", precio);
                        command.Parameters.AddWithValue("@ProveedorID", proveedorID);
                        command.Parameters.AddWithValue("@Vigente", true); // Siempre se agrega como "true"
                        command.Parameters.AddWithValue("@CantidadDisponible", cantidadDisponible); // Cantidad disponible
                        command.Parameters.AddWithValue("@EmpleadoID", empleadoID); // EmpleadoID para obtener la Sucursal

                        // Ejecutar el comando
                        int filasAfectadas = command.ExecuteNonQuery();

                        if (filasAfectadas > 0)
                        {
                            MessageBox.Show("Producto agregado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Limpiar los campos después de agregar el producto
                            txtNombreProducto.Clear();
                            txtCategoriaProducto.Clear();
                            txtPrecioProducto.Clear();
                            txtProveedorIDProducto.Clear();
                            txtCantidadDisponible.Clear();
                        }
                        else
                        {
                            MessageBox.Show("No se pudo agregar el producto. Verifica los datos e inténtalo nuevamente.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error en la base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnRecuperarProveedor_Click(object sender, EventArgs e)
        {
            // Validar que el ID del proveedor no esté vacío
            if (string.IsNullOrWhiteSpace(txtProveedorID.Text))
            {
                MessageBox.Show("Por favor, ingresa un ID de proveedor válido.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int proveedorID;
            // Intentar convertir el texto del ID de proveedor a un entero
            if (!int.TryParse(txtProveedorID.Text.Trim(), out proveedorID) || proveedorID <= 0)
            {
                MessageBox.Show("Por favor, ingresa un ID de proveedor válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("RecuperarProveedor", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Agregar el parámetro ProveedorID
                        command.Parameters.AddWithValue("@ProveedorID", proveedorID);

                        // Ejecutar el procedimiento almacenado
                        command.ExecuteNonQuery();

                        MessageBox.Show("Proveedor y productos recuperados exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (SqlException ex)
            {
                // Manejar errores en la base de datos
                MessageBox.Show($"Error en la base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                // Manejar cualquier otro tipo de error
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnDescontinuarProducto_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar si el ProductoID es válido
                if (string.IsNullOrWhiteSpace(txtProductoID.Text))
                {
                    MessageBox.Show("Por favor ingresa el ID del producto.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int productoID = int.Parse(txtProductoID.Text.Trim()); // Obtener el ProductoID desde el TextBox

                // Llamar al procedimiento almacenado para descontinuar el producto
                string query = "EXEC DescontinuarProducto @ProductoID";

                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    // Crear un SqlCommand para ejecutar el procedimiento
                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@ProductoID", productoID); // Pasar el ProductoID como parámetro

                    int rowsAffected = cmd.ExecuteNonQuery(); // Ejecutar el procedimiento

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("El producto ha sido descontinuado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("No se encontró el producto o ya está descontinuado.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error en la base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnVolverProducto_Click(object sender, EventArgs e)
        {
            try
            {
                // Validar que el usuario haya iniciado sesión
                if (string.IsNullOrWhiteSpace(txtProductoID.Text))
                {
                    MessageBox.Show("Debes proporcionar el ID del producto.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int productoID = int.Parse(txtProductoID.Text.Trim()); // ID del producto a volver a marcar como vigente

                // Consulta al procedimiento almacenado
                string query = "EXEC VolverProducto @ProductoID";

                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ProductoID", productoID);

                        // Ejecutar la consulta
                        command.ExecuteNonQuery();
                    }
                }

                // Mostrar mensaje de éxito
                MessageBox.Show("Producto marcado como vigente correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Opcional: Si deseas actualizar la vista de productos después de cambiar el estado
                // Puedes llamar al código de btnMisProductos o similar para recargar los productos.
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error en la base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnResurtir_Click(object sender, EventArgs e)
        {
            try
            {
                // Validar que los campos necesarios estén completos
                if (string.IsNullOrWhiteSpace(txtUsuario.Text) ||
                    string.IsNullOrWhiteSpace(txtResurtirProductoID.Text) ||
                    string.IsNullOrWhiteSpace(txtResurtirCantidad.Text))
                {
                    MessageBox.Show("Debes completar todos los campos: Usuario, ProductoID y Cantidad.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int empleadoID = int.Parse(txtUsuario.Text.Trim()); // ID del empleado actual
                int productoID = int.Parse(txtResurtirProductoID.Text.Trim()); // ID del producto a resurtir
                int cantidad = int.Parse(txtResurtirCantidad.Text.Trim()); // Cantidad a agregar

                // Consulta al procedimiento almacenado
                string query = "EXEC ResurtirProducto @EmpleadoID, @ProductoID, @Cantidad";

                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Pasar los parámetros al procedimiento almacenado
                        command.Parameters.AddWithValue("@EmpleadoID", empleadoID);
                        command.Parameters.AddWithValue("@ProductoID", productoID);
                        command.Parameters.AddWithValue("@Cantidad", cantidad);

                        // Ejecutar la consulta
                        command.ExecuteNonQuery();
                    }
                }

                // Mostrar mensaje de éxito
                MessageBox.Show("Producto resurtido correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Opcional: Actualizar el DataGridView para reflejar los cambios en disponibilidad
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error en la base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (FormatException)
            {
                MessageBox.Show("Los campos ProductoID y Cantidad deben ser números enteros válidos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnAgregarLista_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar y agregar columnas si no están configuradas
                if (dgvLista.Columns.Count == 0)
                {
                    // Limpiar las columnas existentes si no están configuradas
                    dgvLista.Columns.Clear();
                    dgvLista.Columns.Add("ProductoID", "Producto ID");
                    dgvLista.Columns.Add("Nombre", "Nombre");
                    dgvLista.Columns.Add("Precio", "Precio");
                    dgvLista.Columns.Add("Cantidad", "Cantidad");
                    dgvLista.Columns.Add("Subtotal", "Subtotal");
                }

                // Validar entradas
                if (string.IsNullOrWhiteSpace(txtProductoIDVenta.Text) || string.IsNullOrWhiteSpace(txtVentaCantidad.Text))
                {
                    MessageBox.Show("Debes ingresar el ID del Producto y la Cantidad.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int productoID = int.Parse(txtProductoIDVenta.Text.Trim());
                int cantidad = int.Parse(txtVentaCantidad.Text.Trim());

                // Consulta al procedimiento almacenado para obtener los datos del producto
                string query = "EXEC ObtenerProductoParaVenta @ProductoID";

                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ProductoID", productoID);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string nombre = reader["Nombre"].ToString();
                                decimal precio = (decimal)reader["Precio"];

                                // Agregar datos al DataGridView
                                dgvLista.Rows.Add(productoID, nombre, precio, cantidad, precio * cantidad);
                            }
                            else
                            {
                                MessageBox.Show("Producto no encontrado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }

                // Mostrar el total de la venta
                decimal totalVenta = 0;

                foreach (DataGridViewRow row in dgvLista.Rows)
                {
                    totalVenta += Convert.ToDecimal(row.Cells["Subtotal"].Value);
                }

                lblTotal.Text = "Total: $" + totalVenta.ToString("0.00");
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error en la base de datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (FormatException)
            {
                MessageBox.Show("Los campos ProductoID y Cantidad deben ser números válidos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnConfirmarVenta_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvLista.Rows.Count == 0)
                {
                    MessageBox.Show("No hay productos en la lista.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int empleadoID = int.Parse(txtUsuario.Text.Trim());  // Obtener el ID del empleado
                int sucursalID = GetSucursalIDByEmpleadoID(empleadoID);  // Obtener el SucursalID correspondiente al empleado

                // Insertar la venta en la tabla Venta
                string queryVenta = "INSERT INTO Venta (SucursalID, EmpleadoID, FechaVenta, Total) OUTPUT INSERTED.VentaID VALUES (@SucursalID, @EmpleadoID, @FechaVenta, @Total)";

                using (SqlConnection connection = new SqlConnection(cadenaConexion))
                {
                    connection.Open();

                    // Iniciar la transacción
                    SqlTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        // Insertar la venta y obtener el ID de la venta recién insertada
                        SqlCommand commandVenta = new SqlCommand(queryVenta, connection, transaction);
                        commandVenta.Parameters.AddWithValue("@SucursalID", sucursalID);
                        commandVenta.Parameters.AddWithValue("@EmpleadoID", empleadoID);
                        commandVenta.Parameters.AddWithValue("@FechaVenta", DateTime.Now);
                        commandVenta.Parameters.AddWithValue("@Total", lblTotal.Text.Replace("Total: $", "").Trim());

                        int ventaID = (int)commandVenta.ExecuteScalar();  // Obtener el ID de la venta insertada

                        // Insertar los productos vendidos
                        string queryVentaProducto = "INSERT INTO VentaProducto (VentaID, ProductoID, Cantidad, Precio) VALUES (@VentaID, @ProductoID, @Cantidad, @Precio)";
                        SqlCommand commandVentaProducto = new SqlCommand(queryVentaProducto, connection, transaction);

                        foreach (DataGridViewRow row in dgvLista.Rows)
                        {
                            if (row.IsNewRow) continue; // Ignorar la fila nueva

                            int productoID = Convert.ToInt32(row.Cells["ProductoID"].Value);
                            int cantidad = Convert.ToInt32(row.Cells["Cantidad"].Value);
                            decimal precio = Convert.ToDecimal(row.Cells["Precio"].Value);

                            // Verificar disponibilidad de producto en la sucursal
                            if (!VerificarDisponibilidadSucursal(productoID, sucursalID, cantidad))
                            {
                                MessageBox.Show($"No hay suficiente stock para el producto {productoID}.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                transaction.Rollback();  // Deshacer la transacción
                                return;
                            }

                            // Insertar el producto en la tabla VentaProducto
                            commandVentaProducto.Parameters.Clear();
                            commandVentaProducto.Parameters.AddWithValue("@VentaID", ventaID);
                            commandVentaProducto.Parameters.AddWithValue("@ProductoID", productoID);
                            commandVentaProducto.Parameters.AddWithValue("@Cantidad", cantidad);
                            commandVentaProducto.Parameters.AddWithValue("@Precio", precio);
                            commandVentaProducto.ExecuteNonQuery();

                            // Restar la cantidad de producto disponible en la sucursal
                            ActualizarDisponibilidadSucursal(productoID, sucursalID, cantidad);
                        }

                        // Confirmar la transacción
                        transaction.Commit();
                        MessageBox.Show("Venta realizada con éxito.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Limpiar la lista y actualizar el total
                        dgvLista.Rows.Clear();
                        lblTotal.Text = "Total: $0.00";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();  // Deshacer la transacción en caso de error
                        MessageBox.Show($"Error al procesar venta: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private bool VerificarDisponibilidadSucursal(int productoID, int sucursalID, int cantidad)
        {
            string query = "SELECT Cantidad FROM Disponibilidad WHERE ProductoID = @ProductoID AND SucursalID = @SucursalID";
            using (SqlConnection connection = new SqlConnection(cadenaConexion))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ProductoID", productoID);
                command.Parameters.AddWithValue("@SucursalID", sucursalID);

                object result = command.ExecuteScalar();

                if (result != DBNull.Value)
                {
                    int cantidadDisponible = Convert.ToInt32(result);
                    return cantidadDisponible >= cantidad;  // Verificar si hay suficiente stock
                }
            }

            return false;  // No hay disponibilidad suficiente
        }
        private void ActualizarDisponibilidadSucursal(int productoID, int sucursalID, int cantidad)
        {
            string query = "UPDATE Disponibilidad SET Cantidad = Cantidad - @Cantidad WHERE ProductoID = @ProductoID AND SucursalID = @SucursalID";
            using (SqlConnection connection = new SqlConnection(cadenaConexion))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Cantidad", cantidad);
                command.Parameters.AddWithValue("@ProductoID", productoID);
                command.Parameters.AddWithValue("@SucursalID", sucursalID);
                command.ExecuteNonQuery();
            }
        }
        private int GetSucursalIDByEmpleadoID(int empleadoID)
        {
            string query = "SELECT SucursalID FROM Empleado WHERE EmpleadoID = @EmpleadoID";
            using (SqlConnection connection = new SqlConnection(cadenaConexion))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@EmpleadoID", empleadoID);
                object result = command.ExecuteScalar();

                if (result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
            }

            throw new Exception("No se pudo obtener la sucursal del empleado.");
        }
        private void btnVentasMes_Click(object sender, EventArgs e)
        {
            try
            {
                // Recuperar el mes y año desde el DateTimePicker
                int mes = dmpMostrarVentas.Value.Month;
                int anio = dmpMostrarVentas.Value.Year;

                // Crear una ruta temporal para el archivo PDF
                string tempFolder = Path.GetTempPath();
                string pdfPath = Path.Combine(tempFolder, "Ventas_" + anio + "_" + mes + ".pdf");

                // Crear el escritor y documento PDF
                using (PdfWriter writer = new PdfWriter(pdfPath))
                using (PdfDocument pdf = new PdfDocument(writer))
                {
                    // Crear un documento PDF
                    Document document = new Document(pdf);

                    // Crear una fuente en negrita
                    PdfFont fontBold = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

                    // Agregar el título al documento en negrita
                    document.Add(new Paragraph("Reporte de Ventas del Mes " + mes + " / " + anio)
                        .SetFont(fontBold)
                        .SetFontSize(14)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                    // Consulta al procedimiento almacenado para obtener las ventas del mes y año específico de la sucursal del usuario
                    string query = "EXEC ObtenerVentasMes @EmpleadoID, @Mes, @Anio";

                    using (SqlConnection connection = new SqlConnection(cadenaConexion))
                    {
                        connection.Open();
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@EmpleadoID", int.Parse(txtUsuario.Text));
                            command.Parameters.AddWithValue("@Mes", mes);
                            command.Parameters.AddWithValue("@Anio", anio);

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                // Crear la tabla en el PDF
                                Table table = new Table(7); // 7 columnas: VentaID, FechaVenta, ProductoID, Nombre, Cantidad, Precio, Subtotal
                                table.AddHeaderCell("VentaID");
                                table.AddHeaderCell("FechaVenta");
                                table.AddHeaderCell("ProductoID");
                                table.AddHeaderCell("Nombre");
                                table.AddHeaderCell("Cantidad");
                                table.AddHeaderCell("Precio");
                                table.AddHeaderCell("Subtotal");

                                while (reader.Read())
                                {
                                    // Leer datos y agregar filas a la tabla
                                    table.AddCell(reader["VentaID"].ToString());
                                    table.AddCell(reader["FechaVenta"].ToString());
                                    table.AddCell(reader["ProductoID"].ToString());
                                    table.AddCell(reader["Nombre"].ToString());
                                    table.AddCell(reader["Cantidad"].ToString());
                                    table.AddCell(reader["Precio"].ToString());
                                    table.AddCell(reader["Subtotal"].ToString());
                                }

                                // Añadir la tabla al documento
                                document.Add(table);
                            }
                        }
                    }

                    // Cerrar el documento (esto finaliza el archivo PDF)
                    document.Close();
                }

                // Verificar si el archivo fue generado correctamente
                if (File.Exists(pdfPath))
                {
                    // Intentar abrir el archivo PDF con la aplicación predeterminada
                    ProcessStartInfo startInfo = new ProcessStartInfo(pdfPath)
                    {
                        UseShellExecute = true
                    };

                    // Asegúrate de abrir el archivo con el visor predeterminado
                    Process process = Process.Start(startInfo);

                    // Esperar a que el proceso del PDF termine (cuando se cierra el PDF)
                    process.WaitForExit();
                }
                else
                {
                    MessageBox.Show("No se pudo generar el archivo PDF.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar el reporte: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnVentasYear_Click(object sender, EventArgs e)
        {
            try
            {
                // Recuperar el año desde el DateTimePicker
                int anio = dmpMostrarVentas.Value.Year;

                // Crear una ruta temporal para el archivo PDF
                string tempFolder = Path.GetTempPath();
                string pdfPath = Path.Combine(tempFolder, "Ventas_" + anio + ".pdf");

                // Crear el escritor y documento PDF
                using (PdfWriter writer = new PdfWriter(pdfPath))
                using (PdfDocument pdf = new PdfDocument(writer))
                {
                    // Crear un documento PDF
                    Document document = new Document(pdf);

                    // Crear una fuente en negrita
                    PdfFont fontBold = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

                    // Agregar el título al documento en negrita
                    document.Add(new Paragraph("Reporte de Ventas del Año " + anio)
                        .SetFont(fontBold)
                        .SetFontSize(14)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                    // Consulta al procedimiento almacenado para obtener las ventas del año específico de la sucursal del usuario
                    string query = "EXEC ObtenerVentasAño @EmpleadoID, @Anio";

                    using (SqlConnection connection = new SqlConnection(cadenaConexion))
                    {
                        connection.Open();
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@EmpleadoID", int.Parse(txtUsuario.Text));
                            command.Parameters.AddWithValue("@Anio", anio);

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                // Crear la tabla en el PDF
                                Table table = new Table(7); // 7 columnas: VentaID, FechaVenta, ProductoID, Nombre, Cantidad, Precio, Subtotal
                                table.AddHeaderCell("VentaID");
                                table.AddHeaderCell("FechaVenta");
                                table.AddHeaderCell("ProductoID");
                                table.AddHeaderCell("Nombre");
                                table.AddHeaderCell("Cantidad");
                                table.AddHeaderCell("Precio");
                                table.AddHeaderCell("Subtotal");

                                while (reader.Read())
                                {
                                    // Leer datos y agregar filas a la tabla
                                    table.AddCell(reader["VentaID"].ToString());
                                    table.AddCell(reader["FechaVenta"].ToString());
                                    table.AddCell(reader["ProductoID"].ToString());
                                    table.AddCell(reader["Nombre"].ToString());
                                    table.AddCell(reader["Cantidad"].ToString());
                                    table.AddCell(reader["Precio"].ToString());
                                    table.AddCell(reader["Subtotal"].ToString());
                                }

                                // Añadir la tabla al documento
                                document.Add(table);
                            }
                        }
                    }

                    // Cerrar el documento (esto finaliza el archivo PDF)
                    document.Close();
                }

                // Verificar si el archivo fue generado correctamente
                if (File.Exists(pdfPath))
                {
                    // Intentar abrir el archivo PDF con la aplicación predeterminada
                    ProcessStartInfo startInfo = new ProcessStartInfo(pdfPath)
                    {
                        UseShellExecute = true
                    };

                    // Asegúrate de abrir el archivo con el visor predeterminado
                    Process process = Process.Start(startInfo);

                    // Esperar a que el proceso del PDF termine (cuando se cierra el PDF)
                    process.WaitForExit();
                }
                else
                {
                    MessageBox.Show("No se pudo generar el archivo PDF.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar el reporte: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




    }
}
