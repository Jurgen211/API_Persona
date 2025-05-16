using API_Persona.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_Persona.Data;
//using API_Persona.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

namespace API_Persona.Controllers
{
    //Endpoints de Persona CRUD
    [Route("api/[controller]")]
    [ApiController]
    public class PersonasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PersonasController> _logger;

        public PersonasController(ApplicationDbContext context, ILogger<PersonasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Personas
        // Con este Endpoint se obtienen todas las personas en la base de datos.
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Persona>>> GetPersonas()
        {
            _logger.LogInformation("Obteniendo todas las personas");
            return await _context.Personas.ToListAsync();
        }

        // GET: api/Personas/5
        // Con este Endpoint se obtienen las personas mediante su id en la base de datos.
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Persona>> GetPersona(int id)
        {
            _logger.LogInformation("Buscando persona con ID: {Id}", id);

            var persona = await _context.Personas.FindAsync(id);

            if (persona == null)
            {
                _logger.LogWarning("Persona con ID: {Id} no encontrada", id);
                return NotFound(new { 
                    message = $"No se encontr� ninguna persona con el ID: {id}" });
            }

            return Ok(persona);
        }

        // GET: api/Personas/buscar
        // Con este Endpoint se obtienen todas las personas en la base de datos en la coincidan 
        // o se asemejen los datos de nombre , apellido y email, se pueden buscar cada uno solo o en conjunto.
        [HttpGet("buscar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<Persona>>> BuscarPersonas(
            [FromQuery] string? nombre = null,
            [FromQuery] string? apellido = null,
            [FromQuery] string? email = null)
        {
            // Validar que al menos un par�metro de b�squeda fue proporcionado
            if (string.IsNullOrWhiteSpace(nombre) &&
                string.IsNullOrWhiteSpace(apellido) &&
                string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Intento de b�squeda sin proporcionar criterios");
                return BadRequest(new { 
                    message = "Debe proporcionar al menos un criterio de b�squeda (nombre, apellido o email)" });
            }

            _logger.LogInformation("Buscando personas con filtros - Nombre: {Nombre}, Apellido: {Apellido}, Email: {Email}",
                nombre ?? "no especificado",
                apellido ?? "no especificado",
                email ?? "no especificado");

            // Construir la consulta de forma dinamica
            IQueryable<Persona> query = _context.Personas;

            if (!string.IsNullOrWhiteSpace(nombre))
            {
                // Busqueda insensible a mayusculas/minusculas con LIKE
                query = query.Where(p => EF.Functions.ILike(p.Nombre, $"%{nombre}%"));
            }

            if (!string.IsNullOrWhiteSpace(apellido))
            {
                query = query.Where(p => EF.Functions.ILike(p.Apellido, $"%{apellido}%"));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(p => EF.Functions.ILike(p.Email, $"%{email}%"));
            }

            // Ejecutar la consulta y obtener los resultados
            var personas = await query.ToListAsync();

            _logger.LogInformation("Se encontraron {Count} personas que coinciden con los criterios de b�squeda", personas.Count);

            return Ok(personas);
        }

        // POST: api/Personas
        // Con este Endpoint se agregan personas en la base de datos.
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Persona>> PostPersona(Persona persona)
        {
            try
            {
                // Forzar que el Id sea 0 para asegurar autoincremento
                persona.Id = 0;
                persona.FechaRegistro = DateTime.UtcNow;

                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Persona creada con ID: {Id}", persona.Id);

                return CreatedAtAction(nameof(GetPersona), new { id = persona.Id }, persona);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al crear la persona");
                return BadRequest(new { 
                    message = "Error al crear la persona", error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // PUT: api/Personas/id
        // Con este Endpoint se modifican los datos de las personas en la base de datos.
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutPersona(int id, Persona persona)
        {
            if (id != persona.Id)
            {
                _logger.LogWarning("ID proporcionado ({Id}) no coincide con el ID del objeto ({PersonaId})", id, persona.Id);
                return BadRequest(new { message = "El ID proporcionado en la URL no coincide con el ID del objeto" });
            }

            // Busca la persona para preservar FechaRegistro
            var existingPersona = await _context.Personas.FindAsync(id);
            if (existingPersona == null)
            {
                _logger.LogWarning("Intento de actualizar una persona inexistente con ID: {Id}", id);
                return NotFound(new { message = $"No se encontr� ninguna persona con el ID: {id}" });
            }

            // Preserva la fecha de registro original
            persona.FechaRegistro = existingPersona.FechaRegistro;

            // Desconecta la entidad existente
            _context.Entry(existingPersona).State = EntityState.Detached;

            _context.Entry(persona).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Persona con ID: {Id} actualizada correctamente", id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!PersonaExists(id))
                {
                    _logger.LogWarning("Persona con ID: {Id} no encontrada durante la actualizaci�n", id);
                    return NotFound(new { message = $"No se encontr� ninguna persona con el ID: {id}" });
                }
                else
                {
                    _logger.LogError(ex, "Error de concurrencia al actualizar persona con ID: {Id}", id);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { message = "Error de concurrencia al actualizar", error = ex.Message });
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al actualizar persona con ID: {Id}", id);
                return BadRequest(new { message = "Error al actualizar la persona", error = ex.InnerException?.Message ?? ex.Message });
            }

            return NoContent();
        }

        // DELETE: api/Personas/5
        //Con este endpoint se eliminan los datos de la persona por su id.
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePersona(int id)
        {
            var persona = await _context.Personas.FindAsync(id);
            if (persona == null)
            {
                _logger.LogWarning("Intento de eliminar una persona inexistente con ID: {Id}", id);
                return NotFound(new { 
                    message = $"No se encontr� ninguna persona con el ID: {id}" });
            }

            try
            {
                _context.Personas.Remove(persona);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Persona con ID: {Id} eliminada correctamente", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al eliminar persona con ID: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { 
                        message = "Error al eliminar la persona", error = ex.InnerException?.Message ?? ex.Message });
            }

            return NoContent();
        }

        private bool PersonaExists(int id)
        {
            return _context.Personas.Any(e => e.Id == id);
        }
    }


}
