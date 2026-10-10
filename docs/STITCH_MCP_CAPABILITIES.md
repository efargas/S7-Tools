# Capacidades de la Integración Stitch MCP

La integración con **Stitch MCP** permite la generación, gestión e iteración de diseños de interfaz de usuario (UI) asistidos por inteligencia artificial directamente mediante herramientas MCP.

---

## 🛠️ Herramientas y Capacidades Disponibles

### 1. Gestión de Proyectos
- **`stitch_list_projects`**: Consulta la lista de todos los proyectos creados o compartidos en tu cuenta de Stitch.
- **`stitch_get_project`**: Obtiene detalles específicos de un proyecto (título, fechas, tema visual asociado, capturas de pantalla, etc.).
- **`stitch_create_project`**: Crea un nuevo proyecto en Stitch que sirva como contenedor para pantallas y elementos de UI.
- **`stitch_delete_project`**: Elimina un proyecto existente (requiere confirmación implícita/explícita).

---

### 2. Generación y Edición de Pantallas UI
- **`stitch_generate_screen_from_text`**: Genera pantallas UI completas (para móvil, escritorio o tablet) a partir de prompts de texto/lenguaje natural.
- **`stitch_get_screen`**: Recupera el estado, detalles y recursos de una pantalla específica mediante su ID.
- **`stitch_list_screens`**: Lista todas las pantallas generadas dentro de un proyecto.
- **`stitch_edit_screens`**: Modifica y actualiza pantallas existentes utilizando instrucciones en texto.
- **`stitch_generate_variants`**: Genera variaciones de diseño sobre pantallas existentes explorando distintas alternativas estéticas o funcionales.

---

### 3. Sistemas de Diseño y Temas Visuales
- **`stitch_create_design_system`**: Configura y crea tokens de diseño globales (paleta de colores, tipografía, redondeo de esquinas, modo claro/oscuro y reglas en Markdown).
- **`stitch_update_design_system`**: Actualiza el sistema de diseño de un proyecto para mantener consistencia visual.
- **`stitch_list_design_systems`**: Muestra los sistemas de diseño asociados a un proyecto o globales.
- **`stitch_apply_design_system`**: Aplica un sistema de diseño o tema a un conjunto de pantallas seleccionadas.
- **`stitch_upload_design_md`**: Permite cargar un archivo `DESIGN.md` base64 para definir especificaciones de diseño.
- **`stitch_create_design_system_from_design_md`**: Crea un sistema de diseño directamente desde las reglas del `DESIGN.md` subido.

---

## 💡 Flujos de Trabajo Típicos

1. **Creación de Maquetas Rápidas (Prototipado UI):**
   A partir de una descripción del problema o feature, puedo generar un nuevo proyecto y pantallas UI en dispositivos móviles o de escritorio.

2. **Aplicación de Marca y Consistencia:**
   Podemos definir o personalizar la paleta de colores, tipografías y reglas de layout para aplicar automáticamente el sistema de diseño a todas las pantallas generadas.

3. **Exploración de Alternativas de Diseño:**
   Generar múltiples variantes de una pantalla existente para comparar layouts, jerarquías de información o elementos visuales.
