using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaggeredGrid
{
    const int U_COMPONENT = 0;
    const int V_COMPONENT = 1;

    static internal double AdvectScalar(Fluid fluid, double[,] field, int x, int y) {
        double xOffset = .5f;
        double yOffset = .5f;

        double u = (fluid.u[x,y] + fluid.u[x+1, y]) / 2;
        double v = (fluid.v[x,y] + fluid.v[x, y+1]) / 2;

        double[] parcelIndex = CalculateParcelIndex(x, y, xOffset, yOffset, u, v, fluid.h, fluid.dt);
        parcelIndex = RestrictParcelToDomain(parcelIndex, fluid.cellsX, fluid.cellsY);

        return SampleFieldByIndex(field, parcelIndex);
    }


    static internal double[] AdvectVelocity(Fluid fluid, int x, int y) {
        double u_new = AdvectUVComponent(fluid, U_COMPONENT, x, y);
        double v_new = AdvectUVComponent(fluid, V_COMPONENT, x, y);
        double[] uv = {u_new, v_new};
        return uv;
    }


    static double AdvectUVComponent(Fluid fluid, int component, int x, int y)
    {
        double u;
        double v;
        double[,] field;
        double xOffset = 0;
        double yOffset = 0;

        if (component == U_COMPONENT) {
            yOffset = 0.5;
            field = fluid.u;
            u = fluid.u[x,y];
            v = AverageV(fluid, x, y);
        }
        else if (component == V_COMPONENT) {
            xOffset = 0.5;
            field = fluid.v;
            u = AverageU(fluid, x, y);
            v = fluid.v[x,y];
        }
        else {
            Debug.Log(component + " is not a valid component in StaggeredGrid.AdvectUVComponent.");
            // Including the below assignments so Visual Studio stops complaining about unassigned variables.
            // There's probably a better way to handle it.
            u = 0;
            v = 0;
            field = fluid.u;
        }

        double[] parcelIndex = CalculateParcelIndex(x, y, xOffset, yOffset, u, v, fluid.h, fluid.dt);
        parcelIndex = RestrictParcelToDomain(parcelIndex, fluid.cellsX, fluid.cellsY);

        return SampleFieldByIndex(field, parcelIndex);
    }


    static double[] CalculateParcelIndex(int x, int y, double xOffset, double yOffset, double u, double v, double h, double dt) {
        double xPos = (x + xOffset) * h;  // xPos is in physical simulation space while x is index space
        double yPos = (y + yOffset) * h;

        double xPosParcel = xPos - u * dt;
        double yPosParcel = yPos - v * dt;

        double xParcel = xPosParcel / h - xOffset;
        double yParcel = yPosParcel / h - yOffset;

        double[] parcelIndex = {xParcel, yParcel};

        return parcelIndex;
    }


    static double[] RestrictParcelToDomain(double[] parcelIndex, int cellsX, int cellsY) {
        if (parcelIndex[0] < 1) {
            parcelIndex[0] = 1;
        }
        else if (parcelIndex[0] > cellsX - 2) {
            parcelIndex[0] = cellsX - 2;
        }
        if (parcelIndex[1] < 1) {
            parcelIndex[1] = 1;
        }
        else if (parcelIndex[1] > cellsY - 2) {
            parcelIndex[1] = cellsY - 2;
        }

        return parcelIndex;
    }


    static double SampleFieldByIndex(double[,] field, double[] parcelIndex) {
        int xLeft = (int) parcelIndex[0];
        int xRight = xLeft + 1;
        int yBottom = (int) parcelIndex[1];
        int yTop = yBottom + 1;

        double xToLeft = parcelIndex[0] - xLeft;
        double xToRight = 1 - xToLeft;
        double yToBottom = parcelIndex[1] - yBottom;
        double yToTop = 1 - yToBottom;

        double value = xToRight * yToTop * field[xLeft, yBottom]
                     + xToLeft * yToTop * field[xRight, yBottom]
                     + xToRight * yToBottom * field[xLeft, yTop]
                     + xToLeft * yToBottom * field[xRight, yTop];

        return value;
    }


    static double AverageU(Fluid fluid, int x, int y) {
        return (fluid.u[x,y] + fluid.u[x, y+1] + fluid.u[x-1, y] + fluid.u[x-1, y+1]) / 4;
    }


    static double AverageV(Fluid fluid, int x, int y) {
        return (fluid.v[x,y] + fluid.v[x+1, y] + fluid.v[x, y-1] + fluid.v[x+1, y-1]) / 4;
    }


}
