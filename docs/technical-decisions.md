# Decisiones Técnicas - Integración TMS-OMS

Este documento explica **por qué** la integración está construida como está. Cubre el entregable
de **justificación de patrones** y deja registradas las interpretaciones del enunciado, los
supuestos y las limitaciones conocidas.

La arquitectura y sus componentes se describen en [diagram.md](diagram.md).

1. [Justificación de patrones](#1-justificación-de-patrones)
2. [Interpretaciones del enunciado](#2-interpretaciones-del-enunciado)
3. [Supuestos](#3-supuestos)
4. [Limitaciones conocidas](#4-limitaciones-conocidas)

---

## 1. Justificación de patrones

### Resumen

| Patrón | Tipo | Requisitos |
|---|---|---|
| [Webhook](#11-webhook) | Integración | 1 |
| [Idempotent Receiver](#12-idempotent-receiver) | Integración | 1, 3, 8 |
| [Envelope Wrapper](#13-envelope-wrapper) | Integración | 1 |
| [Message Translator](#14-message-translator) | Integración | 1, 7 |
| [Publish-Subscribe Channel](#15-publish-subscribe-channel) | Integración | 5, 6, 7, 8 |
| [Message Filter](#16-message-filter) | Integración | 6 |
| [Retry con backoff exponencial](#17-retry-con-backoff-exponencial) | Integración | 8 |
| [Dead Letter Channel](#18-dead-letter-channel) | Integración | 8 |
| [Transactional Outbox](#19-transactional-outbox) | Integración | 8 |
| [Clean Architecture](#110-clean-architecture) | Diseño | Todos |
| [Aggregate y Domain Events](#111-aggregate-y-domain-events) | Diseño | 2, 3, 4, 5 |
| [Strategy](#112-strategy) | Diseño | 7 |

Los nombres de los patrones de integración siguen el catálogo de *Enterprise Integration
Patterns* (Hohpe y Woolf).

### Patrones de integración

#### 1.1 Webhook

- **Requisito:** 1. Recibir los eventos del TMS en tiempo real.
- **Problema:** la integración necesita enterarse de cada cambio de estado apenas ocurre.
- **Solución:** el TMS hace un `POST` a `/api/webhooks/tms/events` cada vez que ocurre un evento. La API autentica, valida, descarta duplicados, publica y responde `202 Accepted` sin procesar la lógica de negocio.
- **Por qué este patrón:** el TMS avisa solo cuando hay una novedad, sin consultas inútiles ni demoras.
- **Alternativa descartada:** consultar al TMS cada cierto tiempo (polling). Con miles de envíos diarios serían miles de consultas sin resultado, y cada cambio se conocería con retraso.
- **Tradeoff:** la integración no controla cuándo, cuántas veces ni en qué orden llegan los eventos. Por eso el endpoint responde rápido (para evitar que el TMS reenvíe por timeout), exige una API key y descarta duplicados.

#### 1.2 Idempotent Receiver

- **Requisitos:** 1 (recepción), 3 (contador de visitas) y 8 (consistencia).
- **Problema:** los webhooks se entregan *al menos una vez*: si el TMS no recibe la respuesta, reenvía el mismo evento. Un `NOT DELIVERED` duplicado sumaría una visita de más, y podría disparar un `TO BE RETURN` con solo dos visitas reales.
- **Solución:** como la trama no incluye un identificador de evento, se calcula una clave SHA-256 con `orderNumber`, `serviceType`, `status`, `subStatus` y `eventDate`. `TryRegisterAsync` registra la clave en una sola operación atómica: si ya existía, el evento se descarta y se responde `202` igual. Si la publicación falla después de registrar la clave, se libera para que el reenvío del TMS no se descarte por error.
- **Por qué este patrón:** el enunciado no menciona los duplicados, pero los reenvíos del webhook y los reintentos del requisito 8 los producen. Sin deduplicación, el requisito 3 se corrompe.
- **Alternativa descartada:** comprobar si la clave existe y después agregarla, en dos pasos. Dos reenvíos simultáneos pasarían los dos la comprobación (condición de carrera).
- **Tradeoff:** dos eventos distintos con exactamente los mismos campos se tratarían como duplicados. Es un caso improbable, porque `eventDate` tiene precisión de segundos.

#### 1.3 Envelope Wrapper

- **Requisito:** 1.
- **Problema:** los suscriptores necesitan metadatos técnicos (la clave del evento, la hora de recepción) que no forman parte de la trama del TMS.
- **Solución:** `TmsEventReceived` envuelve al evento de negocio (`TmsEvent`) junto con `EventKey` y `ReceivedAt`.
- **Por qué este patrón:** separa los datos de negocio de los datos técnicos. La clave se calcula una sola vez y la reutilizan todos los suscriptores.
- **Alternativa descartada:** agregar los metadatos como campos de `TmsEvent`, mezclando lo que dice el TMS con lo que agrega la integración.

#### 1.4 Message Translator

- **Requisitos:** 1 (entrada) y 7 (salida).
- **Problema:** el TMS usa sus propios valores (por ejemplo, `"AT PICKUP POINT"`, con espacios), y cada cliente espera las notificaciones con su propia estructura.
- **Solución:** en la entrada, `TmsCodes` traduce los valores del TMS a los enums del dominio. En la salida, cada formateador traduce el modelo neutro `OrderStatusNotification` a la estructura de su cliente.
- **Por qué este patrón:** el dominio no conoce el formato del TMS ni el de ningún cliente. Un cambio de formato afecta a una sola clase.
- **Alternativa descartada:** poner atributos de serialización en los enums del dominio, lo que haría depender a la capa más interna del formato JSON de un sistema externo.

#### 1.5 Publish-Subscribe Channel

- **Requisitos:** 5 (historial), 6 (evidencias), 7 (notificación) y 8 (reintentos independientes).
- **Problema:** un mismo evento dispara tres efectos independientes. Si uno falla o tarda, los demás no deberían verse afectados.
- **Solución:** un bus con dos tópicos: eventos recibidos y eventos procesados. Cada suscriptor tiene su propio canal y su propio worker, con sus propios reintentos (*fan-out*). Las suscripciones se registran al iniciar la aplicación.
- **Por qué este patrón:** la descarga lenta de una foto no demora el historial, y una notificación fallida se reintenta sin repetir las demás. Es coreografía: cada suscriptor reacciona por su cuenta.
- **Alternativas descartadas:**
  - **Procesamiento síncrono en el webhook:** la respuesta al TMS dependería del paso más lento, y un fallo en cualquier paso haría fallar todo el evento.
  - **Observer en memoria:** es síncrono, comparte el hilo y no permite reintentar un suscriptor por separado.
  - **Orquestación (Saga):** los efectos no dependen entre sí ni necesitan compensarse, así que un orquestador central sería complejidad sin beneficio.
  - **Un solo canal compartido:** cada mensaje llegaría a un único suscriptor (*Competing Consumers*), no a todos.
- **Tradeoff:** los efectos secundarios son eventualmente consistentes: ocurren unos milisegundos después de actualizar el pedido.

#### 1.6 Message Filter

- **Requisito:** 6. Las evidencias se guardan solo en seis hitos.
- **Problema:** todos los eventos procesados llegan al suscriptor de evidencias, pero solo algunos deben almacenarlas.
- **Solución:** `EvidenceMilestoneFilter` deja pasar un evento solo si su estado es uno de los seis hitos (`COLLECTED`, `NOT COLLECTED`, `DELIVERED`, `NOT DELIVERED`, `RETURNED`, `NOT RETURNED`), si trae evidencias y si se aplicó al pedido.
- **Por qué este patrón:** la regla del requisito queda en un único lugar, separada de la descarga y el almacenamiento.
- **Alternativa descartada:** *Content-Based Router*. Un router elige entre varios destinos según el contenido; acá hay un único destino y la decisión es pasar o no pasar.

#### 1.7 Retry con backoff exponencial

- **Requisito:** 8. Gestionar automáticamente los fallos temporales.
- **Problema:** una caída momentánea de la red o del servidor de evidencias no debería perder datos ni requerir intervención manual.
- **Solución:** Polly en dos niveles:

  | Nivel | Intentos | Esperas | Reintenta |
  |---|---|---|---|
  | Descarga HTTP de evidencias | 4 | ~2s, ~4s, ~8s | 5xx, 408, 429, errores de red y timeouts por intento (10s). No reintenta los 4xx |
  | Cada suscriptor del bus | 4 | ~1s, ~2s, ~4s | Cualquier error, salvo el apagado de la aplicación |

  Cada reintento de un suscriptor usa un scope de inyección de dependencias nuevo, para no arrastrar estado de un intento fallido.
- **Por qué este patrón:** el backoff exponencial le da tiempo al servicio para recuperarse, y el *jitter* reparte los reintentos para que muchos fallos simultáneos no reintenten en el mismo instante.
- **Alternativa descartada:** reintentos con espera fija, que vuelven a saturar a un servicio que ya está en problemas.
- **Tradeoff:** se distinguen los errores **transitorios** de los **permanentes**. Una URL mal formada (`htps://`) o un 404 no se arreglan reintentando, así que no se reintentan a nivel HTTP.

#### 1.8 Dead Letter Channel

- **Requisito:** 8. Evitar la pérdida de datos.
- **Problema:** un mensaje que agota todos los reintentos se perdería.
- **Solución:** el worker lo guarda en una cola de mensajes fallidos con el mensaje completo en JSON, el suscriptor que falló, el error y la fecha. Si guardarlo también fallara, el worker lo registra como error crítico y sigue procesando.
- **Por qué este patrón:** el mensaje queda disponible para revisarlo o reprocesarlo. Al registrar el suscriptor, se puede reprocesar solo el efecto que falló, sin repetir los demás.

#### 1.9 Transactional Outbox

- **Requisito:** 8. Garantizar la consistencia.
- **Problema:** guardar el pedido y publicar sus eventos son dos operaciones. Si la publicación falla después de guardar, el pedido cambia pero el historial, las evidencias y la notificación nunca se enteran.
- **Solución:** el handler guarda el pedido y el mensaje en el Outbox en la misma unidad de trabajo. Un `OutboxDispatcher` en segundo plano publica los mensajes pendientes y solo los quita después de publicarlos.
- **Por qué este patrón:** un fallo en la publicación deja el mensaje pendiente para el siguiente intento. La entrega es *al menos una vez*: puede haber un duplicado, pero nunca una pérdida.
- **Alternativa descartada:** una transacción distribuida entre la base de datos y el broker, que la mayoría de los brokers no soporta y agrega complejidad y acoplamiento.

### Patrones de diseño

#### 1.10 Clean Architecture

- **Problema:** el enunciado permite reemplazar los servicios externos por implementaciones en memoria, pero el diseño debe poder evolucionar a servicios reales.
- **Solución:** cuatro proyectos con las dependencias hacia el centro: Domain no depende de nada, Application define los **puertos** (interfaces) e Infrastructure los **implementa**.
- **Por qué este patrón:** reemplazar el bus en memoria por un broker real, o los repositorios por una base de datos, solo modifica Infrastructure. Las reglas de negocio y los casos de uso no cambian.

#### 1.11 Aggregate y Domain Events

- **Requisitos:** 2, 3, 4 y 5.
- **Problema:** el estado final, el contador de visitas y el `TO BE RETURN` automático son reglas que deben cumplirse siempre, sin importar quién modifique el pedido.
- **Solución:** `Order` es un aggregate con propiedades de solo lectura y un único método, `Apply`, para cambiar su estado. En lugar de llamar a otros componentes, registra lo ocurrido como domain events (`OrderStatusChanged` y `OrderEventRejected`) que después se publican.
- **Por qué este patrón:** las reglas no se pueden saltear, y el dominio no conoce el historial, las evidencias ni las notificaciones.
- **Alternativa descartada:** Observer dentro del dominio. El aggregate no tiene suscriptores: solo acumula hechos que se publican después de guardar.

#### 1.12 Strategy

- **Requisito:** 7. Cada cliente tiene su propia estructura de notificación.
- **Solución:** cada cliente con formato propio tiene un `INotificationFormatter`, y un formateador por defecto cubre a los demás. `NotificationFormatterResolver` elige el formateador según el código del cliente.
- **Por qué este patrón:** agregar un cliente es agregar una clase y registrarla. El handler y el resolvedor no cambian (principio abierto/cerrado).
- **Alternativa descartada:** un `switch` por código de cliente dentro del handler, que crecería con cada cliente nuevo.

---

## 2. Interpretaciones del enunciado

| Tema | Interpretación | Motivo |
|---|---|---|
| Estados finales | Solo `DELIVERED` y `RETURNED` bloquean nuevos eventos. No se valida una tabla completa de transiciones | Es lo que pide el requisito 2. Una tabla estricta podría rechazar eventos reales del TMS, por ejemplo si se pierde un `STARTED` |
| Contador de visitas | Solo `DELIVERED` y `NOT DELIVERED` suman una visita | Son los únicos estados en los que el courier llegó al domicilio |
| Devolución automática | El `TO BE RETURN` se emite cuando la tercera visita es un `NOT DELIVERED`. Si es un `DELIVERED`, el pedido ya es final | Devolver un pedido entregado contradice el requisito 2 |
| Momento de la devolución | El `TO BE RETURN` automático se aplica en la misma operación que el tercer `NOT DELIVERED`, y se marca como automático | Un pedido nunca puede quedar con tres visitas fallidas sin pasar a devolución |
| Fase de devolución | `TO BE RETURN` pertenece a la fase de devolución | "Asignado a devolución" ya forma parte de ese proceso |
| Historial | Se registran también los eventos rechazados, con su motivo | El requisito 5 pide registrar *cada* evento |
| Evidencias | No se guardan las evidencias de los eventos rechazados | No corresponden a ningún cambio del pedido. El rechazo igual queda en el historial |
| Notificación | Los eventos rechazados no se notifican. La tercera visita fallida genera dos notificaciones: `NOT DELIVERED` y `TO BE RETURN` | Para el cliente, un evento rechazado no cambia nada |
| Datos automáticos | El `TO BE RETURN` automático no hereda `subStatus`, courier, vehículo ni receptor del evento que lo provocó | Ningún courier lo reportó |
| Sub estados | `subStatus` se acepta como texto libre | El enunciado no define sus valores posibles |
| Pedidos desconocidos | Un evento de un pedido que no existe en el OMS se ignora y se registra en el log | El OMS es la fuente de verdad de los pedidos; la integración no los crea |

---

## 3. Supuestos

| Tema | Supuesto |
|---|---|
| Fecha del evento | `eventDate` llega en formato `yyyy-MM-dd HH:mm:ss`, sin zona horaria, y está en hora de Lima (UTC-5). Se usa un desfase fijo, porque Perú no tiene horario de verano |
| Campos obligatorios | `serviceType`, `status`, `details.orderNumber` y `eventDate`. El resto es opcional |
| Valores del TMS | `status`, `serviceType` y `dispatchType` se comparan sin distinguir mayúsculas |
| Identificación del evento | La combinación de pedido, tipo de servicio, estado, sub estado y fecha identifica un único evento |
| Autenticación del webhook | El TMS envía una API key en el header `X-Api-Key`. Si el TMS firma sus tramas (por ejemplo, con HMAC), validar la firma sería más seguro |
| Cliente del pedido | El código de cliente se toma del pedido en el OMS; el de la trama del TMS se usa solo si el OMS no lo tiene |
| Respuesta a duplicados | El TMS deja de reenviar un evento cuando recibe un `2xx` |

---

## 4. Limitaciones conocidas

| Tema | Limitación | Mejora posible |
|---|---|---|
| Persistencia en memoria | Pedidos, historial, evidencias, mensajes pendientes y claves se pierden al reiniciar | Base de datos y servicios externos reales |
| Atomicidad del Outbox | En memoria no se puede demostrar que el pedido y el mensaje se guardan en la misma transacción | Tabla Outbox en la misma base de datos que los pedidos |
| Claves de idempotencia | No expiran, así que la memoria crece con cada evento | Caché distribuida con expiración |
| Duplicados | La entrega *al menos una vez* puede repetir una notificación. En memoria, reintentar `ProcessTmsEventHandler` podría aplicar un evento dos veces, porque el repositorio devuelve la misma instancia | Registrar en el pedido las claves de los eventos ya aplicados |
| Eventos desordenados | Se guarda la fecha del último evento, pero no se descartan los eventos más antiguos | Ignorar los eventos con `eventDate` anterior al último aplicado |
| `TO BE RETURN` duplicado | Si el TMS envía su propio `TO BE RETURN` después del automático, se aplica y se notifica otra vez | Ignorar un cambio al mismo estado |
| Reintentos multiplicados | En el peor caso, una descarga se intenta 16 veces (4 HTTP × 4 del suscriptor), y el suscriptor reintenta también los errores permanentes | Excluir los errores 4xx del reintento del suscriptor |
| Tamaño de evidencias | No hay un límite de tamaño de descarga | Rechazar archivos grandes según `Content-Length` |
| Consulta del historial | El endpoint no tiene autenticación | Autenticación propia, por ejemplo con JWT |
| Formatos de notificación | Se definen en código | Plantillas por cliente como configuración |
| Cola de mensajes fallidos | No hay un endpoint para consultarla ni reprocesar sus mensajes | Reprocesamiento desde el broker o un endpoint administrativo |
