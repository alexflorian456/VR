using System;

namespace rt
{
    class RayTracer
    {
        private Geometry[] geometries;
        private Light[] lights;

        public RayTracer(Geometry[] geometries, Light[] lights)
        {
            this.geometries = geometries;
            this.lights = lights;
        }

        private double ImageToViewPlane(int n, int imgSize, double viewPlaneSize)
        {
            return -n * viewPlaneSize / imgSize + viewPlaneSize / 2;
        }

        private Intersection FindFirstIntersection(Line ray, double minDist, double maxDist)
        {
            var intersection = Intersection.NONE;

            foreach (var geometry in geometries)
            {
                var intr = geometry.GetIntersection(ray, minDist, maxDist);

                if (!intr.Valid || !intr.Visible) continue;

                if (!intersection.Valid || !intersection.Visible)
                {
                    intersection = intr;
                }
                else if (intr.T < intersection.T)
                {
                    intersection = intr;
                }
            }

            return intersection;
        }

        private bool IsLit(Vector point, Light light, Ellipsoid ellipsoid)
        {
            Line line = new Line(point, light.Position);
            foreach (var geometry in geometries)
            {
                if(!(geometry is RawCtMask)){
                    Ellipsoid ellipsoid2 = (Ellipsoid)geometry;
                    if ((ellipsoid2.Center - ellipsoid.Center).Length() < 0.001) // skip the sphere the point is on
                    {
                        continue;
                    }
                
                    // other spheres:
                    Intersection intersection = ellipsoid2.GetIntersection(line, 0, 1000);
                    if (intersection.T > 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        
        private double Relu(double x)
        {
            if (x < 0) return 0;
            return x;
        }

        public void Render(Camera camera, int width, int height, string filename)
        {
            var background = new Color(0.2, 0.2, 0.2, 1.0);
            
            var viewParallel = (camera.Up ^ camera.Direction).Normalize();

            var image = new Image(width, height);

            var vecW = camera.Direction * camera.ViewPlaneDistance;

            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var dx = vecW + viewParallel * ImageToViewPlane(i, width, camera.ViewPlaneWidth)
                                  + camera.Up * ImageToViewPlane(j, height, camera.ViewPlaneHeight);
                    dx.Normalize();

                    var line = new Line();
                    line.X0 = camera.Position;
                    line.Dx = dx;

                    var intersection = FindFirstIntersection(line, camera.FrontPlaneDistance, camera.BackPlaneDistance);

                    if (intersection.Valid)
                    {
                        Geometry geometry = intersection.Geometry;
                        if(geometry is RawCtMask){
                            var color = new Color();
                            foreach (var light in lights)
                            {
                                var V = intersection.Position;
                                var N = intersection.Normal;
                                var L = light.Position;
                                var T = (L - V).Normalize();
                                var R = (N * (N * T) * 2 - T).Normalize();
                                var E = (camera.Position - V).Normalize();

                                color += intersection.Material.Ambient * light.Ambient +
                                         intersection.Material.Diffuse * light.Diffuse * Relu(N * T) +
                                         intersection.Material.Specular * light.Specular *
                                         Math.Pow(Relu(E * R), intersection.Material.Shininess);
                            }
                            image.SetPixel(i,j,color);
                        }
                        else{
                            var color = new Color();
                            foreach (var light in lights)
                            {
                                var V = intersection.Position;
                                var N = intersection.Normal;
                                var L = light.Position;
                                var T = (L - V).Normalize();
                                var R = (N * (N * T) * 2 - T).Normalize();
                                var E = (camera.Position - V).Normalize();

                                if (IsLit(V, light, (Ellipsoid)geometry))
                                {
                                    color += geometry.Material.Ambient * light.Ambient +
                                             geometry.Material.Diffuse * light.Diffuse * Relu(N * T) +
                                             geometry.Material.Specular * light.Specular *
                                             Math.Pow(Relu(E * R), geometry.Material.Shininess);
                                }
                                else
                                {
                                    color += geometry.Material.Ambient * light.Ambient;
                                }
                            }

                            image.SetPixel(i, j, color);
                        }
                    }
                    else
                    {
                        image.SetPixel(i, j, background);
                    }
                }
            }

            image.Store(filename);
        }
    }
}