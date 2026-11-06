using System.Collections.Generic;

namespace Naval.Core
{
    /// <summary>
    /// The routes the player owns. The network starts at the start port. A new route must touch it;
    /// the first free_routes routes cost nothing.
    /// </summary>
    public sealed class Network
    {
        readonly GameData data;
        readonly List<Route> owned = new List<Route>();
        readonly HashSet<Port> ports = new HashSet<Port>();

        public int FreeRoutesLeft { get; private set; }
        public IReadOnlyList<Route> Owned { get { return owned; } }

        public Network(GameData data)
        {
            this.data = data;
            FreeRoutesLeft = data.Rules.FreeRoutes;
            ports.Add(data.StartPort);
        }

        public bool Owns(Route route)
        {
            return owned.Contains(route);
        }

        /// <summary>True for the start port and every port an owned route touches.</summary>
        public bool Reaches(Port port)
        {
            return ports.Contains(port);
        }

        public bool CanConnect(Route route)
        {
            return ports.Contains(route.A) || ports.Contains(route.B);
        }

        /// <summary>0 while free routes remain, otherwise the route's price.</summary>
        public int CostOf(Route route)
        {
            return FreeRoutesLeft > 0 ? 0 : route.Price;
        }

        /// <summary>Adds the route. The caller has checked <see cref="CanConnect"/> and taken payment.</summary>
        public void Add(Route route)
        {
            owned.Add(route);
            ports.Add(route.A);
            ports.Add(route.B);
            if (FreeRoutesLeft > 0) FreeRoutesLeft--;
        }

        /// <summary>
        /// The legs of the shortest owned path, in order, or null if there is none.
        /// Ocean routes are skipped unless allowed.
        /// </summary>
        public List<Route> ShortestPath(Port from, Port to, bool allowOcean)
        {
            int count = data.Ports.Count;
            var distance = new double[count];
            var arrivedBy = new Route[count];
            var done = new bool[count];
            for (int i = 0; i < count; i++) distance[i] = double.PositiveInfinity;
            distance[from.Index] = 0;
            while (true)
            {
                int u = -1;
                for (int i = 0; i < count; i++)
                    if (!done[i] && !double.IsPositiveInfinity(distance[i]) && (u < 0 || distance[i] < distance[u])) u = i;
                if (u < 0 || u == to.Index) break;
                done[u] = true;
                foreach (Route route in owned)
                {
                    if (route.Ocean && !allowOcean) continue;
                    if (route.A.Index != u && route.B.Index != u) continue;
                    int v = route.Other(data.Ports[u]).Index;
                    if (distance[u] + route.Distance < distance[v])
                    {
                        distance[v] = distance[u] + route.Distance;
                        arrivedBy[v] = route;
                    }
                }
            }
            if (double.IsPositiveInfinity(distance[to.Index])) return null;
            var path = new List<Route>();
            Port at = to;
            while (at != from)
            {
                Route leg = arrivedBy[at.Index];
                path.Add(leg);
                at = leg.Other(at);
            }
            path.Reverse();
            return path;
        }
    }
}
