using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using System.IO;
using System.Text;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [System.Serializable] //Guardar datos
    public class PalabraObjeto // Clase para asociar palabra y objeto
    {
        public string palabraEnIngles;
        public GameObject objetoPrefab;
        public GameObject objetoInstanciado;
        public Quaternion rotacionInicialObjeto;
    }
    // Listas de palabras y objetos
    public List<PalabraObjeto> palabrasYObjetos = new List<PalabraObjeto>(); //Lista de palabras
    public List<TextMeshPro> textosDeCajas = new List<TextMeshPro>();
    public List<Transform> posicionesDeCajas = new List<Transform>();
    public GameObject luzCorrectoPrefab;
    public Transform parentObjetosInstanciados; //referencia de posiciones
    public GameObject botonInicioJuego;
    public GameObject panelFinDeJuego;
    public TextMeshProUGUI textoFinDeJuego;
    private List<PalabraObjeto> palabrasMastered = new List<PalabraObjeto>();
    private List<PalabraObjeto> palabrasIncorrectas = new List<PalabraObjeto>(); // Lista para guardar las palabras incorrectas en rondas anteriores
    private List<PalabraObjeto> palabrasEnRonda = new List<PalabraObjeto>();//Palabras seleccionadas para la ronda actual
    private Dictionary<string, GameObject> palabraACaja = new Dictionary<string, GameObject>(); //Diccionario para asociar palabras con GameObjects de las cajas de texto
    private bool juegoEnCurso = false;
    private Dictionary<string, Vector3> posicionesInicialesObjetos = new Dictionary<string, Vector3>();// Diccionario para guardar las posiciones iniciales de los objetos
    private bool errorOcurridoEnEstaRonda = false;


    //----TIEMPO---
    public TextMeshProUGUI textoTiempoFinDeJuego;
    private float tiempoInicioJuego;
    private float tiempoTotalJuego;

    //---ARCHIVO---
    private string fileRoute;
    private string dataToSave = "";
    private int numeroRonda = 1;
    public string dataType;

    void Start()
    {
        panelFinDeJuego.SetActive(false);
        botonInicioJuego.SetActive(true);


        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        fileRoute = Application.persistentDataPath + "/KitchenRounds" + timestamp + ".txt";

        if (!File.Exists(fileRoute))
        {
            string header = "Object" + "\t" + "State" + "\n";
            File.WriteAllText(fileRoute, header);
            dataType = "";
        }
        else
        {
            string header = "Object" + "\t" + "State" + "\n";
            File.WriteAllText(fileRoute, header);
            dataType = ""; ;
        }
        Debug.Log("Se creó el txt correctamente en: " + fileRoute);
        StartCoroutine(SaveData());

        foreach (PalabraObjeto po in palabrasYObjetos) // Recorre la lista de palabras y objetos definidos
        {
            if (po.objetoInstanciado != null)
            {
                //Añade al diccionario la palabra en inglés y la posición inicial del objeto
                posicionesInicialesObjetos.Add(po.palabraEnIngles, po.objetoInstanciado.transform.position);
                po.rotacionInicialObjeto = po.objetoInstanciado.transform.rotation;
            }
        }
        Debug.Log("<color=green>Start() - Posiciones iniciales de objetos guardadas.</color>");
    }
    // Coroutine para guardar los datos 
    IEnumerator SaveData()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(5);
            if (!string.IsNullOrEmpty(dataToSave))
            {
                File.AppendAllText(fileRoute, dataToSave);
                dataToSave = "";
            }
        }
    }
    void SaveImmediate()
    {
        if (!string.IsNullOrEmpty(dataToSave))
        {
            File.AppendAllText(fileRoute, dataToSave);
            dataToSave = "";
        }
    }
    public void AddData(string ronda, string objeto, string estado)
    {
        if (!string.IsNullOrEmpty(ronda))
        {
            dataToSave += ronda + "\n";
        }
        dataToSave += objeto + "\t" + estado + "\n";
    }
    public void IniciarJuego()
    {
        // Guarda el tiempo en el que el juego comenzo
        tiempoInicioJuego = Time.time;
        // Verifica que haya al menos 5 palabras y objetos definidos
        if (palabrasYObjetos.Count < 5)
        {
            Debug.LogError("Necesitas al menos 5 palabras y objetos definidos.");
            return;
        }

        juegoEnCurso = true;
        botonInicioJuego.SetActive(false);
        panelFinDeJuego.SetActive(false);
        // Limpia la lista de palabras incorrectas de rondas anteriores
        palabrasIncorrectas.Clear();
        foreach (PalabraObjeto po in palabrasYObjetos)
        {
            if (po.objetoInstanciado != null)
            {
                po.objetoInstanciado.SetActive(true); // activa el objeto al inicio del juego
                po.objetoInstanciado.transform.position = posicionesInicialesObjetos[po.palabraEnIngles]; // Re-posiciona el objeto a su posicion inicial
                po.objetoInstanciado.transform.rotation = po.rotacionInicialObjeto;
                Debug.Log($"<color=blue>IniciarJuego() - Activando y Re-posicionando Objeto al inicio del juego: {po.palabraEnIngles}, Posicion: {posicionesInicialesObjetos[po.palabraEnIngles]}</color>");
            }
        }

        IniciarNuevaRonda();
    }
    void IniciarNuevaRonda()
    {
        foreach (PalabraObjeto po in palabrasYObjetos)
        {
            if (po.objetoInstanciado != null)
            {
                po.objetoInstanciado.transform.SetParent(parentObjetosInstanciados);
                if (posicionesInicialesObjetos.ContainsKey(po.palabraEnIngles))
                {
                    po.objetoInstanciado.transform.position = posicionesInicialesObjetos[po.palabraEnIngles];
                    po.objetoInstanciado.transform.rotation = po.rotacionInicialObjeto;
                }
            }
        }
        AddData("Round " + numeroRonda, "", "");
        numeroRonda++;

        // Se crea una nueva lista para almacenar las palabras de la ronda actual
        palabrasEnRonda = new List<PalabraObjeto>();
        // Si hay palabras incorrectas de rondas anteriores, se usaran esas primero
        List<PalabraObjeto> palabrasParaSeleccionar = (palabrasIncorrectas.Count > 0) ? palabrasIncorrectas : palabrasYObjetos;
        // Si no hay palabras disponibles y el juego sigue en curso significa que se ha completado todo el juego
        palabraACaja.Clear(); // Limpia el diccionario palabraACaja antes de la nueva ronda
        if (palabrasParaSeleccionar.Count == 0 && palabrasIncorrectas.Count == 0 && juegoEnCurso)
        {
            FinDelJuego();
            return;
        }
        else if (palabrasParaSeleccionar.Count < 5)
        {
            palabrasEnRonda.AddRange(palabrasParaSeleccionar);// Añade todas las palabras disponibles a la ronda
            int palabrasFaltantes = 5 - palabrasEnRonda.Count;
            if (palabrasFaltantes > 0)
            {
                //List<PalabraObjeto> palabrasCorrectasRestantes = palabrasYObjetos.FindAll(p => !palabrasEnRonda.Contains(p));
                List<PalabraObjeto> palabrasCorrectasRestantes = palabrasYObjetos.FindAll(p => !palabrasEnRonda.Contains(p) && !palabrasMastered.Contains(p));
                for (int i = 0; i < palabrasFaltantes && i < palabrasCorrectasRestantes.Count; i++)
                {
                    palabrasEnRonda.Add(palabrasCorrectasRestantes[i]);// Agrega palabras adicionales hasta completar 5
                }
            }
        }
        else
        {
            //List<PalabraObjeto> seleccionAleatoria = new List<PalabraObjeto>(palabrasParaSeleccionar);

            List<PalabraObjeto> seleccionAleatoria = palabrasParaSeleccionar.Where(p => !palabrasMastered.Contains(p)).ToList();
            for (int i = 0; i < 5; i++)
            {
                int indiceAleatorio = Random.Range(0, seleccionAleatoria.Count);
                palabrasEnRonda.Add(seleccionAleatoria[indiceAleatorio]);// Se agrega la palabra seleccionada a la lista de la ronda
                seleccionAleatoria.RemoveAt(indiceAleatorio); // Se elimina la palabra 
            }
        }
        errorOcurridoEnEstaRonda = false;
        // Asigna las palabras seleccionadas a las cajas y reposiciona los objetos
        for (int i = 0; i < 5; i++)
        {
            // Verifica que haya suficientes cajas y posiciones disponibles para la ronda
            if (i < textosDeCajas.Count && i < posicionesDeCajas.Count && i < palabrasEnRonda.Count)
            {
                textosDeCajas[i].text = palabrasEnRonda[i].palabraEnIngles; //asigna las palabras a la caja
                textosDeCajas[i].color = Color.white;
                // Añade al diccionario la asociación entre la palabra y el GameObject de la caja
                palabraACaja.Add(palabrasEnRonda[i].palabraEnIngles, textosDeCajas[i].transform.parent.gameObject);//asigna la pal a la caja correcta
                // Si el objeto de la palabra está instanciado se reubica en su posicion inicial
                if (palabrasEnRonda[i].objetoInstanciado != null)
                {
                    // Aqui  se restaura el objeto a la posicion inical
                    if (posicionesInicialesObjetos.ContainsKey(palabrasEnRonda[i].palabraEnIngles))
                    {
                        palabrasEnRonda[i].objetoInstanciado.transform.position = posicionesInicialesObjetos[palabrasEnRonda[i].palabraEnIngles];
                        palabrasEnRonda[i].objetoInstanciado.transform.rotation = palabrasEnRonda[i].rotacionInicialObjeto;
                        Debug.Log($"<color=blue>IniciarNuevaRonda() - Objeto: {palabrasEnRonda[i].palabraEnIngles}, Re-posicionando a posicion guardada: {posicionesInicialesObjetos[palabrasEnRonda[i].palabraEnIngles]}</color>");
                    }
                    else
                    {

                        Debug.LogWarning($"<color=yellow>IniciarNuevaRonda() - No hay posicion inicial guardada para: {palabrasEnRonda[i].palabraEnIngles}. Manteniendo posicion actual.</color>");
                    }
                    // Se activa el objeto para que sea visible en la nueva ronda
                    palabrasEnRonda[i].objetoInstanciado.SetActive(true);
                }
            }
        }
    }

    public void ValidarRespuestas()
    {
        if (!juegoEnCurso) return;

        bool todasCorrectas = true;

        for (int i = 0; i < palabrasEnRonda.Count; i++)
        {
            string palabraCaja = palabrasEnRonda[i].palabraEnIngles;
            GameObject cajaGameObject = palabraACaja[palabraCaja];
            TextMeshPro textoCajaComponent = cajaGameObject.GetComponentInChildren<TextMeshPro>();
            Renderer cajaRenderer = cajaGameObject.GetComponent<Renderer>();
            XRGrabInteractable objetoEnCajaInteractable = null;
            Collider[] collidersEnCaja = cajaGameObject.GetComponentsInChildren<Collider>();

            // Buscamos el objeto en la caja
            foreach (Collider cajaCollider in collidersEnCaja)
            {
                if (cajaCollider.isTrigger)
                {
                    Collider[] objetosEnTrigger = Physics.OverlapBox(cajaCollider.bounds.center, cajaCollider.bounds.extents);
                    foreach (Collider objetoCollider in objetosEnTrigger)
                    {
                        XRGrabInteractable grabInteractable = objetoCollider.GetComponent<XRGrabInteractable>();
                        if (grabInteractable != null && grabInteractable.gameObject.activeSelf)
                        {
                            objetoEnCajaInteractable = grabInteractable;
                            break;
                        }
                    }
                    if (objetoEnCajaInteractable != null)
                        break;
                }
            }

            // Validacion del contenido de la caja
            if (objetoEnCajaInteractable != null)
            {
                PalabraObjeto palabraObjetoArrastrado = palabrasYObjetos.Find(po => po.objetoInstanciado == objetoEnCajaInteractable.gameObject);

                if (palabraObjetoArrastrado != null && palabraObjetoArrastrado.palabraEnIngles == palabraCaja)
                {
                    Debug.Log($"<color=green>¡Respuesta CORRECTA para caja: {palabraCaja}!</color>");
                    if (textoCajaComponent != null)
                    {
                        textoCajaComponent.color = Color.green;
                    }
                    // Si estaba en la lista de errores la removemos 
                    if (palabrasIncorrectas.Contains(palabraObjetoArrastrado))
                    {
                        //palabrasIncorrectas.Remove(palabraObjetoArrastrado);
                    }
                    if (!palabrasMastered.Contains(palabraObjetoArrastrado))
                        palabrasMastered.Add(palabraObjetoArrastrado);
                }
                else
                {
                    // Se detecta error en la caja  ronda como fallida
                    todasCorrectas = false;
                    errorOcurridoEnEstaRonda = true;
                    //Registra palabra
                    AddData("", palabraCaja, "Wrong");
                    PalabraObjeto palabraObjetoCorrecto = palabrasYObjetos.Find(po => po.palabraEnIngles == palabraCaja);
                    Debug.Log($"Palabra Objeto Correcto encontrado: {(palabraObjetoCorrecto != null ? palabraObjetoCorrecto.palabraEnIngles : "NULL")}");

                    if (cajaRenderer != null)
                    {
                        StartCoroutine(CambiarColorTemporalmente(cajaRenderer, Color.red, 0.5f, Color.white));
                    }

                    if (palabraObjetoCorrecto != null)
                    {
                        Debug.Log($"<color=orange>Llamando a MostrarParticulasObjetoCorrecto para: {palabraObjetoCorrecto.palabraEnIngles}</color>");
                        MostrarParticulasObjetoCorrecto(palabraObjetoCorrecto);
                        if (!palabrasIncorrectas.Contains(palabraObjetoCorrecto))
                        {
                            palabrasIncorrectas.Add(palabraObjetoCorrecto);
                        }
                    }

                    if (textoCajaComponent != null)
                    {
                        textoCajaComponent.color = Color.red;
                    }
                }
            }
            else
            {
                // La caja está vacía 
                todasCorrectas = false;
                errorOcurridoEnEstaRonda = true;

                if (textoCajaComponent != null)
                {
                    textoCajaComponent.color = Color.red;
                }
                if (cajaRenderer != null)
                {
                    StartCoroutine(CambiarColorTemporalmente(cajaRenderer, Color.red, 0.5f, Color.white));
                }
                PalabraObjeto palabraObjetoCorrecto = palabrasYObjetos.Find(po => po.palabraEnIngles == palabraCaja);
                if (palabraObjetoCorrecto != null)
                {
                    MostrarParticulasObjetoCorrecto(palabraObjetoCorrecto);
                }
            }
        }

        // Si todas las cajas son correctas
        if (todasCorrectas)
        {
            foreach (PalabraObjeto po in palabrasEnRonda)
            {
                AddData("", po.palabraEnIngles, "Good");
            }
            if (errorOcurridoEnEstaRonda)
            {
                // Se completó la ronda con errores
                Debug.Log("<color=yellow>Ronda completada con correcciones. Pasando a nueva ronda.</color>");
                // Reiniciamos el flag para la nueva ronda.
                errorOcurridoEnEstaRonda = false;
                IniciarNuevaRonda();
            }
            else
            {
                // Nunca hubo error en la ronda
                Debug.Log("<color=green>Ronda perfecta, finalizando juego.</color>");
                FinDelJuego();
            }
        }
        else
        {
            Debug.Log("<color=yellow>Existen errores. Corrige los objetos y presiona Validar nuevamente.</color>");
        }
    }


    public void ComprobarObjetoEnCajaDesdeObjeto(string palabraCaja, GameObject objetoArrastrado, string palabraObjetoDelObjeto)
    {
        if (!juegoEnCurso) return;
        // Verifica si la palabra del objeto coincide con la palabra de la caja
        if (palabraObjetoDelObjeto == palabraCaja)
        {
            // Verifica si la caja correspondiente a la palabra esta en el diccionario
            if (palabraACaja.ContainsKey(palabraCaja))
            {
                // Obtiene el componente de texto de la caja correcta
                TextMeshPro textoCajaCorrecta = palabraACaja[palabraCaja].GetComponentInChildren<TextMeshPro>();
                if (textoCajaCorrecta != null)
                {
                    textoCajaCorrecta.color = Color.green;
                }
            }
        }
    }

    void MostrarParticulasObjetoCorrecto(PalabraObjeto palabraObjeto) // Función para mostrar partículas de luz en el objeto correcto
    {
        Debug.Log("<color=magenta>MostrarParticulasObjetoCorrecto() - Funcion llamada</color>");
        // Si el prefab de luz no es asignado no hace nada
        if (luzCorrectoPrefab != null)
        {
            Debug.Log("<color=blue>MostrarParticulasObjetoCorrecto() - luzCorrectoPrefab NO es NULL. Nombre Prefab: " + luzCorrectoPrefab.name + "</color>");
        }
        else
        {
            Debug.LogError("<color=red>MostrarParticulasObjetoCorrecto() - error luzCorrectoPrefab ES NULL.</color>");
            return;
        }

        if (palabraObjeto.objetoPrefab != null) // Si el objeto no está instanciado
        {
            //Debug.Log("<color=blue>MostrarParticulasObjetoCorrecto() - palabraObjeto.objetoInstanciado NO es NULL. Nombre Objeto: " + palabraObjeto.objetoPrefab.name + "</color>");
            Debug.Log("<color=blue>MostrarParticulasObjetoCorrecto() - palabraObjeto.objetoInstanciado NO es NULL. Nombre Objeto: " + palabraObjeto.objetoInstanciado.name + "</color>");
        }
        else
        {
            //Debug.LogError("<color=red>MostrarParticulasObjetoCorrecto() - error palabraObjeto.objetoInstanciado ES NULL para palabra: " + palabraObjeto.palabraEnIngles +" - objeto: "+ palabraObjeto.objetoInstanciado.name +" - prefab: "+palabraObjeto.objetoPrefab.name  +"" + "</color>");
            Debug.LogError("<color=red>MostrarParticulasObjetoCorrecto() - error palabraObjeto.objetoInstanciado ES NULL para palabra: " + palabraObjeto.palabraEnIngles + "</color>");
            return;
        }
        //Debug.Log($"<color=green>MostrarParticulasObjetoCorrecto() - ¡A PUNTO DE ENCENDER FOCO DE LUZ para objeto: {palabraObjeto.objetoPrefab.name}, Palabra: {palabraObjeto.palabraEnIngles}!</color>");
        Debug.Log($"<color=green>MostrarParticulasObjetoCorrecto() - ¡A PUNTO DE ENCENDER FOCO DE LUZ para objeto: {palabraObjeto.objetoInstanciado.name}, Palabra: {palabraObjeto.palabraEnIngles}!</color>");

        // Instancia el prefab de luz spotlight como hijo del objeto correcto
        GameObject luzInstanciada = Instantiate(luzCorrectoPrefab, palabraObjeto.objetoInstanciado.transform);
        luzInstanciada.transform.localPosition = Vector3.up * 2f;
        Destroy(luzInstanciada, 30f);
    }


    void ComprobarRondaCompleta(bool rondaSinErroresInicial)
    {
        if (rondaSinErroresInicial)
        {
            Debug.Log("<color=green>ComprobarRondaCompleta() - ¡Juego completado!</color>");
            FinDelJuego();
        }
        else
        {
            Debug.Log("<color=yellow>ComprobarRondaCompleta() - Hubo errores en la ronda. Nueva ronda.</color>");
            IniciarNuevaRonda();
        }
    }

    void FinDelJuego()
    {
        juegoEnCurso = false;
        panelFinDeJuego.SetActive(true);
        tiempoTotalJuego = Time.time - tiempoInicioJuego;
        int minutos = Mathf.FloorToInt(tiempoTotalJuego / 60f);
        int segundos = Mathf.FloorToInt(tiempoTotalJuego % 60f);
        string mensajeTiempo = string.Format("Total time: {0:00}:{1:00}", minutos, segundos);
        textoFinDeJuego.text = "Game Completed! Do you want to play again?\n" + mensajeTiempo;
        botonInicioJuego.SetActive(false);

        AddData("", "Final time", mensajeTiempo);
        SaveImmediate();
    }

    public void ReiniciarJuego()
    {
        IniciarJuego();
        panelFinDeJuego.SetActive(false);
    }

    IEnumerator CambiarColorTextoTemporalmente(TextMeshPro textoMeshPro, Color colorInicio, float duracion, Color colorFinal)
    {
        if (textoMeshPro != null)
        {
            textoMeshPro.color = colorInicio;
            yield return new WaitForSeconds(duracion);
            textoMeshPro.color = colorFinal;
        }
    }
    IEnumerator CambiarColorTemporalmente(Renderer renderer, Color colorInicio, float duracion, Color colorFinal)
    {
        if (renderer != null)
        {
            Material originalMaterial = renderer.material;
            Material temporaryMaterial = new Material(originalMaterial);
            renderer.material = temporaryMaterial;

            renderer.material.color = colorInicio;
            yield return new WaitForSeconds(duracion);
            renderer.material.color = colorFinal;

            renderer.material = originalMaterial;
        }
    }
}