/* Code adapted from KSP Trajectories mod (https://github.com/neuoy/KSPTrajectories)
 *
 *
 * The MIT License (MIT)
 *
 * Copyright (c) 2014 Youen Toupin, aka neuoy
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 * Except as contained in this notice, the name of the copyright holders shall not
 * be used in advertising or otherwise to promote the sale, use or other dealings
 * in this Software without prior written authorization from the copyright holders.
 */
using System;
using UnityEngine;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class StockAtmosphere
    {
        /// <summary>
        /// Stock atmospheric state at a hypothetical body-relative position and UT.
        /// Pressure is in KSP's native kilopascals; the other quantities are SI.
        /// </summary>
        internal struct AtmosphericState
        {
            public double Altitude;
            public double Pressure;
            public double Temperature;
            public double Density;
            public double SpeedOfSound;
        }

        /// <summary>
        /// Evaluate the KSP 1.12.5 FlightIntegrator atmospheric-state chain once.
        /// The supplied world position provides the hypothetical body-relative radial
        /// direction; UT is used only for the body/Sun ephemeris and seasonal curves.
        /// </summary>
        internal static AtmosphericState GetAtmosphericState(
            Vector3d position, CelestialBody body, double ut)
        {
            var relativePosition = position - body.position;
            var altitude = relativePosition.magnitude - body.Radius;
            if (!body.atmosphere || altitude >= body.atmosphereDepth) {
                return new AtmosphericState {
                    Altitude = altitude,
                    Temperature = PhysicsGlobals.SpaceTemperature
                };
            }

            var pressure = body.GetPressure(altitude);
            if (pressure <= 0d) {
                return new AtmosphericState {
                    Altitude = altitude,
                    Temperature = PhysicsGlobals.SpaceTemperature
                };
            }

            var up = (Vector3)relativePosition.normalized;
            var bodyAxis = body.bodyTransform.up;
            var sun = Planetarium.fetch.Sun;
            var sunDirectionAtUT = (sun.getTruePositionAtUT(ut)
                                    - body.getTruePositionAtUT(ut)).normalized;
            var sunDirection = (Vector3)sunDirectionAtUT;

            // CelestialBody.GetAtmoThermalStats calculates the latitude and the
            // normalization bounds from these two polar angles. Clamp the dot
            // products only against round-off at the acos domain boundary.
            var bodyAxisDot = Math.Max(-1d, Math.Min(1d,
                (double)Vector3.Dot(bodyAxis, up)));
            var sunAxisDot = Math.Max(-1d, Math.Min(1d,
                (double)Vector3.Dot(sunDirection, bodyAxis)));
            var bodyPolarAngle = Math.Acos(bodyAxisDot);
            var sunPolarAngle = Math.Acos(sunAxisDot);
            var maximumSunDot = (1d + Math.Cos(
                sunPolarAngle - bodyPolarAngle)) * 0.5d;
            var minimumSunDot = (1d + Math.Cos(
                sunPolarAngle + bodyPolarAngle)) * 0.5d;

            var phaseRotation = Quaternion.AngleAxis(
                45f * Mathf.Sign((float)body.rotationPeriod), bodyAxis);
            var correctedSunDot = (1d + Vector3.Dot(
                sunDirection, phaseRotation * up)) * 0.5d;
            var sunDotRange = maximumSunDot - minimumSunDot;
            var normalizedSunlight = sunDotRange > 0.001d
                ? (correctedSunDot - minimumSunDot) / sunDotRange
                : minimumSunDot + sunDotRange * 0.5d;

            var foldedPolarAngle = bodyPolarAngle;
            if (foldedPolarAngle > Math.PI / 2d)
                foldedPolarAngle = Math.PI - foldedPolarAngle;
            var latitude = (float)((Math.PI / 2d - foldedPolarAngle)
                                   * 57.29578d);

            // The axial and eccentricity terms use the body in this body's
            // hierarchy that directly orbits the stock system's single Sun.
            var bodyReferencingSun = global::CelestialBody.GetBodyReferencing(
                body, sun);
            var axialPhase = 0f;
            var eccentricityOffset = 0d;
            if (bodyReferencingSun != null && bodyReferencingSun.orbit != null) {
                var orbit = bodyReferencingSun.orbit;
                axialPhase = (float)((orbit.TrueAnomalyAtUT(ut) * 57.29578d
                                      + 360d) % 360d);
                if (orbit.eccentricity != 0d) {
                    var radiusAtUT = orbit.getRelativePositionAtUT(ut).magnitude;
                    eccentricityOffset = body.eccentricityTemperatureBiasCurve.Evaluate(
                        (float)((radiusAtUT - orbit.PeR) / (orbit.ApR - orbit.PeR)));
                }
            }

            var temperatureOffset =
                (double)body.latitudeTemperatureBiasCurve.Evaluate(latitude)
                + (double)body.latitudeTemperatureSunMultCurve.Evaluate(latitude)
                    * normalizedSunlight
                + (double)body.axialTemperatureSunBiasCurve.Evaluate(axialPhase)
                    * body.axialTemperatureSunMultCurve.Evaluate(latitude)
                + eccentricityOffset;
            var temperature = body.GetTemperature(altitude)
                + body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude)
                    * temperatureOffset;
            var density = body.GetDensity(pressure, temperature);
            var speedOfSound = body.GetSpeedOfSound(pressure, density);
            return new AtmosphericState {
                Altitude = altitude,
                Pressure = pressure,
                Temperature = temperature,
                Density = density,
                SpeedOfSound = speedOfSound
            };
        }

        /// <summary>
        /// Gets the exact stock atmospheric temperature at the current UT.
        /// </summary>
        public static double GetTemperature(Vector3d position, CelestialBody body)
        {
            return GetAtmosphericState(
                position, body, Planetarium.GetUniversalTime()).Temperature;
        }

        /// <summary>
        /// Gets the air density (rho) for the specified altitude (above sea level, in meters) on the specified body.
        /// This is an approximation, because actual calculations, taking sun exposure into account to compute air
        /// temperature, require to know the actual point on the body where the density is to be computed
        /// (knowing the altitude is not enough).
        /// However, the difference is small for high altitudes, so it makes very little difference
        /// for trajectory prediction.
        /// </summary>
        public static double GetDensity(double altitude, CelestialBody body)
        {
            if (!body.atmosphere)
                return 0;
            if (altitude > body.atmosphereDepth)
                return 0;
            var pressure = body.GetPressure(altitude);
            // get an average day/night temperature at the equator
            var sunDot = 0.5;
            var sunAxialDot = 0f;
            var atmosphereTemperatureOffset = body.latitudeTemperatureBiasCurve.Evaluate(0) + body.latitudeTemperatureSunMultCurve.Evaluate(0) * sunDot + body.axialTemperatureSunMultCurve.Evaluate(sunAxialDot);
            var temperature = body.GetTemperature(altitude) + body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;
            return body.GetDensity(pressure, temperature);
        }

        /// <summary>
        /// Gets the air pressure (in Pascals) for the specified altitude (above sea level, in meters) on the specified body.
        /// </summary>
        public static double GetPressure (double altitude, CelestialBody body)
        {
            if (!body.atmosphere)
                return 0;
            if (altitude > body.atmosphereDepth)
                return 0;
            return body.GetPressure (altitude) * 1000d;
        }
    }
}
