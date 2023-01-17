using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using static TicketsConsole.Program;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
namespace TicketsConsole
{
    internal class Program
    {
        public const int CLOSEST_CITIES_LIMIT = 5;
        static void Main(string[] args)
        {

            var events = new List<Event>{
                new Event(1, "Phantom of the Opera", "New York", new DateTime(2023,12,23)),
                new Event(2, "Metallica", "Los Angeles", new DateTime(2023,12,02)),
                new Event(3, "Metallica", "New York", new DateTime(2023,12,06)),
                new Event(4, "Metallica", "Boston", new DateTime(2023,10,23)),
                new Event(5, "LadyGaGa", "New York", new DateTime(2023,09,20)),
                new Event(6, "LadyGaGa", "Boston", new DateTime(2023,08,01)),
                new Event(7, "LadyGaGa", "Chicago", new DateTime(2023,07,04)),
                new Event(8, "LadyGaGa", "San Francisco", new DateTime(2023,07,07)),
                new Event(9, "LadyGaGa", "Washington", new DateTime(2023,05,22)),
                new Event(10, "Metallica", "Chicago", new DateTime(2023,01,01)),
                new Event(11, "Phantom of the Opera", "San Francisco", new DateTime(2023,07,04)),
                new Event(12, "Phantom of the Opera", "Chicago", new DateTime(2024,05,15))
            };

            var customer = new Customer()
            {
                Id = 1,
                Name = "John",
                City = "New York",
                BirthDate = new DateTime(1995, 05, 10)
            };

            IMarketingEngine marketingEngine = new MarketingEngine(events);
            Console.WriteLine("Customer Birthday Notification");
            marketingEngine.SendClosestEventToBirtDayNotification(customer);
            Console.WriteLine("");
            Console.WriteLine("Same City Events Customer Notification");
            marketingEngine.SendSameCityEventsNotification(customer);
            Console.WriteLine("");
            Console.WriteLine("Closest Events to Customer Notification");
            marketingEngine.SendClosestEventsToCustomerCity(customer, null);

        }
        public record City(string Name, int X, int Y);
        public record EventDistance(Event e, int distance);
        public static IDictionary<string, int> CityDistanceCache = new Dictionary<string, int>();
        public static readonly IDictionary<string, City> Cities = new Dictionary<string, City>()
        {
            { "New York", new City("New York", 3572, 1455) },
            { "Los Angeles", new City("Los Angeles", 462, 975) },
            { "San Francisco", new City("San Francisco", 183, 1233) },
            { "Boston", new City("Boston", 3778, 1566) },
            { "Chicago", new City("Chicago", 2608, 1525) },
            { "Washington", new City("Washington", 3358, 1320) },
        };
        public class Event
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string City { get; set; }
            public decimal Price { get; set; }
            public DateTime Date { get; set; }
            public Event(int id, string name, string city, DateTime date, decimal price = 0)
            {
                this.Id = id;
                this.Name = name;
                this.City = city;
                this.Date = date;
                this.Price = price;
            }
        }
        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string City { get; set; }
            public DateTime BirthDate { get; set; }

            public DateTime NextBirthDay
            {
                get
                {
                    DateTime currentYearBirthDate = new DateTime(DateTime.Now.Year, BirthDate.Month, BirthDate.Day);
                    return currentYearBirthDate > DateTime.Now ? currentYearBirthDate : currentYearBirthDate.AddYears(1);
                }
            }
        }

        public interface IMarketingEngine
        {
            void SendCustomerNotifications(Customer customer, Event e);
            bool SendSameCityEventsNotification(Customer customer);
            bool SendClosestEventToBirtDayNotification(Customer customer);
            bool SendClosestEventsToCustomerCity(Customer customer, List<string>? sortList, bool shouldSort = false);
        }

        public class MarketingEngine : IMarketingEngine
        {
            private List<Event> MarketingEvents { get; }
            public MarketingEngine(List<Event> events)
            {
                MarketingEvents = events;
            }

            public void SendCustomerNotifications(Customer customer, Event e)
            {
                Console.WriteLine($"{customer.Name} from {customer.City} event {e.Name} at {e.Date} for ${e.Price}");
            }
            public bool SendSameCityEventsNotification(Customer customer)
            {
                foreach (var marketingEvent in MarketingEvents)
                {
                    if (marketingEvent.City == customer.City)
                        SendCustomerNotifications(customer, marketingEvent);
                }
                //simulate successful sending of notification
                return true;
            }

            public bool SendClosestEventToBirtDayNotification(Customer customer)
            {
                Event? closestEventToBithDay = ClosestEventsToDate(customer.NextBirthDay).FirstOrDefault();
                if (closestEventToBithDay != null) { SendCustomerNotifications(customer, closestEventToBithDay); }
                else return false;
                //simulate successful sending of notification
                return true;
            }
            private IEnumerable<Event> ClosestEventsToDate(DateTime? date = null, int limit = 1)
            {
                var effectiveDate = date ?? DateTime.Now;

                var closestEvents = MarketingEvents.Where(e => e.Date >= effectiveDate).OrderBy(e => e.Date).Take(limit);

                return closestEvents;
            }

            public bool SendClosestEventsToCustomerCity(Customer customer, List<string>? sortList = default, bool shouldSort = false)
            {
                sortList = sortList ?? new List<string>();
                var closestEventsToCustomer = ClosestEventsToCustomer(customer);
                //simple sorting for a property
                if (shouldSort)
                    closestEventsToCustomer.OrderBy(e => e.Price);

                //Dynamic sorting
                foreach (var parameterName in sortList)
                {
                    closestEventsToCustomer = closestEventsToCustomer.AsQueryable().OrderBy(parameterName, true);
                }

                //Real life situation, events will be collated into a notification and sent at the same time
                foreach (var closeEvent in closestEventsToCustomer)
                {
                    //Show Event Distances
                    var cityDistanceKey = CityDistanceKey(closeEvent.City, customer.City);
                    var eventDistance = CityDistanceCache.Where(c => c.Key == cityDistanceKey).Single().Value;
                    Console.WriteLine($"Distance from {customer.City} to {closeEvent.City} is {eventDistance}");

                    SendCustomerNotifications(customer, closeEvent);
                }
                //simulate successful sending of notification
                return true;
            }

            private IEnumerable<Event> ClosestEventsToCustomer(Customer customer)
            {
                List<EventDistance> eventDistances = new List<EventDistance>();
                //avoid sending past events
                var futureEvents = MarketingEvents.Where(marketingEvent => marketingEvent.Date > DateTime.Now).ToList();

                foreach (Event marketingEvent in futureEvents)
                {
                    int eventDistance = GetEventDistance(customer, marketingEvent);
                    if (eventDistance < 0) return new List<Event>();
                    eventDistances.Add(new EventDistance(marketingEvent, eventDistance));
                }
                return eventDistances.OrderBy(ed => ed.distance).Take(CLOSEST_CITIES_LIMIT).Select(ed => ed.e).ToList();
            }

            private int GetEventDistance(Customer customer, Event marketingEvent)
            {
                try
                {
                    var customerCityInfo = Cities.Where(c => c.Key == customer.City).Single().Value;
                    var eventCityInfo = Cities.Where(c => c.Key == marketingEvent.City).Single().Value;
                    var cityDistanceKey = CityDistanceKey(customer.City, marketingEvent.City);
                    int eventDistance;
                    if (CityDistanceCache.ContainsKey(cityDistanceKey))
                    {
                        eventDistance = CityDistanceCache.Where(c => c.Key == cityDistanceKey).Single().Value;
                    }
                    else
                    {
                        eventDistance = Math.Abs(customerCityInfo.X - eventCityInfo.X) + Math.Abs(customerCityInfo.Y - eventCityInfo.Y);
                        CityDistanceCache.Add(cityDistanceKey, eventDistance);
                    }
                    return eventDistance;
                }
                catch (WebException ex)
                {
                    HandleWebException(ex);
                    return -1;
                }
            }
            private string CityDistanceKey(string cityNameA, string cityNameB)
            {
                return string.Compare(cityNameA, cityNameB, StringComparison.OrdinalIgnoreCase) < 0
                    ? $"{cityNameA} - {cityNameB}"
                    : $"{cityNameB} - {cityNameA}";
            }

            private void HandleWebException(WebException ex)
            {
                //log exception
                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    Console.WriteLine("The API timed out, please try again later.");
                }
                else if (ex.Response != null)
                {
                    using (HttpWebResponse errorResponse = (HttpWebResponse)ex.Response)
                    {
                        if (errorResponse.StatusCode == HttpStatusCode.NotFound)
                        {
                            Console.WriteLine("City not found, please check the spelling");
                        }
                        else if (errorResponse.StatusCode == HttpStatusCode.Forbidden)
                        {
                            Console.WriteLine("You are not authorized to access this service, please check your credentials.");
                        }
                    }
                }
            }
        }
    }
    public static class Extensions
    {
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string propertyName, bool isAscending = true)
        {
            if (String.IsNullOrEmpty(propertyName))
            {
                return source;
            }

            ParameterExpression parameter = Expression.Parameter(source.ElementType, "");

            MemberExpression property = Expression.Property(parameter, propertyName);
            LambdaExpression lambda = Expression.Lambda(property, parameter);

            string methodName = isAscending ? "OrderBy" : "OrderByDescending";

            Expression methodCallExpression = Expression.Call(typeof(Queryable), methodName,
                                  new Type[] { source.ElementType, property.Type },
                                  source.Expression, Expression.Quote(lambda));

            return source.Provider.CreateQuery<T>(methodCallExpression);
        }
    }
}
