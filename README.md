# Спецификација – PetCare Platform

## 1. Опис на темата и проблемот

PetCare Platform е систем за управување со домашни миленици, ветеринарни термини, медицинска историја, вакцинации и известувања. Целта е сопствениците да имаат едно место за профилите на милениците и термините, додека ветеринарите внесуваат прегледи, дијагнози и терапии. Системот е поделен на три независни bounded contexts, еднакви на бројот на членови во тим од три лица.

## 2. Архитектура

Решението е microservice monorepo изработено со ASP.NET Core Web API и .NET 10. Секој бизнис сервис има Domain, Application, Infrastructure и API слој. Сервисите немаат директни project references еден кон друг и не пристапуваат до туѓа база. Клиентските барања влегуваат преку YARP API Gateway. Keycloak издава OAuth 2.0/OIDC JWT токени, Consul служи како service registry, а Kafka пренесува integration events. MCP серверот ги изложува главните read-only сценарија како AI алатки.

## 3. Domain-Driven Design

### Pet bounded context

Aggregate roots се `Owner` и `Pet`. `Pet` содржи value object `MicrochipNumber`, вид, раса, датум на раѓање, алергии и хронични состојби. Бизнис правилата спречуваат невалиден датум на раѓање, празно име и невалиден број на микрочип. Само Pet Service е сопственик на овие податоци.

### Appointment bounded context

Главни ентитети се `Clinic`, `Veterinarian`, `AvailabilitySlot` и `Appointment`. `AvailabilitySlot` го инкапсулира правилото дека резервиран или изминат термин не може повторно да се резервира. `Appointment` ја контролира транзицијата меѓу Scheduled, Cancelled и Completed и дозволува презакажување само на активен термин.

### Treatment & Notification bounded context

Главни ентитети се `MedicalExamination`, `Vaccination` и `Notification`. Медицинскиот преглед чува дијагноза, терапија, лекови и следна контрола. Вакцинацијата чува датум на давање и следен рок. Известувањето има status и source event identifier за идемпотентна обработка на Kafka пораки.

## 4. Опис на сервисите

### Pet Service

Овозможува CRUD операции за сопственици и миленици. Endpoint-от `GET /api/pets/{id}/exists?ownerId=...` е анти-корупциски интерфејс што Appointment Service го користи без да го преземе Pet домен моделот. Податоците се чуваат во посебна PostgreSQL база `petcare_pet`.

### Appointment Service

Овозможува пребарување слободни ветеринари по датум, локација и специјализација, како и закажување, откажување и презакажување. Пред закажување, `HttpClient` го повикува Pet Service. Logical host-от `pet-service` се разрешува преку Consul, а service-account токен се добива од Keycloak. По успешна промена сервисот објавува Kafka integration event.

### Treatment & Notification Service

Овозможува внесување прегледи и вакцинации и читање медицинска историја. Background Kafka consumer ги обработува AppointmentScheduled, AppointmentCancelled и AppointmentRescheduled настаните. За секој настан создава идемпотентно известување. Друг background worker ги испраќа пристигнатите известувања во demo режим преку console log.  
`TODO:` оваа имплементација може да се замени со email/SMS provider.

## 5. Комуникација меѓу сервисите

Синхроната комуникација е REST преку `HttpClient`. Appointment Service има сопствен `IPetVerificationClient` и `PetServiceClient`, што претставува Anti-Corruption Layer: надворешниот JSON се претвора во локалниот `PetVerificationResult`. Consul ја дава адресата на здрава Pet Service инстанца. Service-to-service повикот носи OAuth client-credentials токен.

Асинхроната комуникација користи Kafka topic `petcare.appointments`. Пораките се обвиткани во `IntegrationEventEnvelope` со event type, payload, timestamp и correlation ID. Consumer-от commit-ира offset само по успешна обработка. Уникатниот `SourceEventId` во Notification базата спречува дупли известувања при повторна испорака.

## 6. API Gateway и service discovery

YARP е централизирана влезна точка. Патеките `/pet`, `/appointment` и `/treatment` се рутираат кон соодветните сервиси, а gateway-от бара автентициран корисник. Во Docker cluster YARP користи внатрешни DNS адреси. Сите компоненти дополнително се регистрираат во Consul. Appointment Service го користи Consul health API за runtime discovery, со што може да избере меѓу повеќе здрави инстанци.

## 7. Безбедност

Keycloak realm-от `petcare` содржи улоги `owner`, `veterinarian`, `admin` и `service`. API сервисите и gateway-от ја валидираат потписаната JWT порака. Controllers користат role authorization: сопственик/администратор менува pet и appointment податоци, а ветеринар/администратор внесува медицински податоци. Appointment Service и MCP серверот користат confidential clients и client-credentials flow.

Demo web client-от користи direct access grant за лесна презентација. Во production треба да се замени со Authorization Code + PKCE, да се овозможи HTTPS, да се валидира audience и client secrets да се чуваат во secret manager.

## 8. Бази и конфигурација

Секој сервис има посебна PostgreSQL база и сопствен EF Core `DbContext`. Нема cross-database joins или споделени табели. При demo старт се користи `EnsureCreated` и seed data за повторлива презентација. Конфигурацијата е во `appsettings.json`, а Docker Compose ја заменува преку environment variables. Sensitive вредности во примерот се само demo secrets и мора да се сменат за production.

## 9. Consumer-driven contracts

Appointment Service е consumer, а Pet Service е provider. Pact consumer test креира mock Pet Service и проверува дека `PetServiceClient` правилно го чита договорот за pet ownership. Тестот генерира Pact JSON. Provider test го репродуцира истиот договор кон вистински Pet Service на TCP endpoint и проверува дека status, headers и body се компатибилни.

## 10. MCP сервер

MCP серверот користи официјален C# SDK и Streamable HTTP transport. Изложени се четири tools: `GetPet`, `GetUpcomingAppointments`, `GetNextVaccination` и `FindAvailableVeterinarians`. MCP серверот не пристапува директно до базите, преку service-account токен го повикува API Gateway, со што ги почитува истите сервисни граници и security правила. Може да се тестира со MCP Inspector или друг MCP client на `http://localhost:7001/mcp`.

## 11. Тестирање и демонстрација

Health endpoints постојат на сите сервиси. `PetCarePlatform.http` и `scripts/demo.ps1` демонстрираат login, читање pet, пребарување слободен термин, закажување и појава на асинхроно известување. Pact тестовите го покриваат критичниот HTTP договор. За финалното видео се користи планот во `VIDEO_SCRIPT.md`, со максимално времетраење од пет минути.

## 12. Улоги во тимот

Член 1 е одговорен за Pet Service и pet domain. Член 2 е одговорен за Appointment Service, Consul и синхроната интеграција. Член 3 е одговорен за Treatment & Notification Service, Kafka и MCP. Gateway, Keycloak, документацијата и integration testing се заедничка одговорност.
