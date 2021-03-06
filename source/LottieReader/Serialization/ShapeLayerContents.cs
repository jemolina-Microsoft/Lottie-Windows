﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using static Microsoft.Toolkit.Uwp.UI.Lottie.LottieData.Serialization.Exceptions;

namespace Microsoft.Toolkit.Uwp.UI.Lottie.LottieData.Serialization
{
#pragma warning disable SA1205 // Partial elements should declare access
#pragma warning disable SA1601 // Partial elements should be documented
    sealed partial class LottieCompositionReader
    {
        ShapeLayerContent ReadShapeContent(in LottieJsonObjectElement obj)
        {
            var args = default(ShapeLayerContent.ShapeLayerContentArgs);
            ReadShapeLayerContentArgs(obj, ref args);

            var type = obj.StringPropertyOrNull("ty") ?? string.Empty;

            switch (type)
            {
                case "gr":
                    return ReadShapeGroup(obj, in args);
                case "st":
                    return ReadSolidColorStroke(obj, in args);
                case "gs":
                    return ReadGradientStroke(obj, in args);
                case "fl":
                    return ReadSolidColorFill(obj, in args);
                case "gf":
                    return ReadGradientFill(obj, in args);
                case "tr":
                    return ReadTransform(obj, in args);
                case "el":
                    return ReadEllipse(obj, in args);
                case "sr":
                    return ReadPolystar(obj, in args);
                case "rc":
                    return ReadRectangle(obj, in args);
                case "sh":
                    return ReadPath(obj, in args);
                case "tm":
                    return ReadTrimPath(obj, in args);
                case "mm":
                    return ReadMergePaths(obj, in args);
                case "rd":
                    return ReadRoundedCorner(obj, in args);
                case "rp":
                    return ReadRepeater(obj, in args);

                // Is this "Offset Paths"?
                case "op":
                default:
                    _issues.UnexpectedValueForType("ShapeContentType", type);
                    return null;
            }
        }

        ShapeGroup ReadShapeGroup(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("cix", "cl", "ix", "hd");

            var numberOfProperties = obj.Int32PropertyOrNull("np");
            var items = ReadShapesList(obj.ArrayPropertyOrNull("it"));
            obj.AssertAllPropertiesRead();
            return new ShapeGroup(in shapeLayerContentArgs, items);
        }

        void ReadShapeLayerContentArgs(
            in LottieJsonObjectElement obj,
            ref ShapeLayerContent.ShapeLayerContentArgs args)
        {
            args.Name = ReadName(obj);
            args.MatchName = ReadMatchName(obj);
            args.BlendMode = BmToBlendMode(obj.DoublePropertyOrNull("bm"));
        }

        // "st"
        SolidColorStroke ReadSolidColorStroke(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("fillEnabled", "hd");

            var color = ReadAnimatableColor(obj.ObjectPropertyOrNull("c"));
            var opacity = ReadAnimatableOpacity(obj.ObjectPropertyOrNull("o"));
            var strokeWidth = ReadAnimatableFloat(obj.ObjectPropertyOrNull("w"));
            var capType = LcToLineCapType(obj.DoublePropertyOrNull("lc"));
            var joinType = LjToLineJoinType(obj.DoublePropertyOrNull("lj"));
            var miterLimit = obj.DoublePropertyOrNull("ml") ?? 4; // Default miter limit in After Effects is 4

            // Get dash pattern to be set as StrokeDashArray
            Animatable<double> offset = null;
            var dashPattern = new List<double>();
            var dashes = obj.ArrayPropertyOrNull("d");
            if (dashes != null)
            {
                var dashesArray = dashes.Value;

                for (int i = 0; i < dashesArray.Count; i++)
                {
                    var dashObj = dashesArray[i].AsObject();

                    switch (dashObj?.StringPropertyOrNull("n"))
                    {
                        case "o":
                            offset = ReadAnimatableFloat(dashObj?.ObjectPropertyOrNull("v"));
                            break;
                        case "d":
                        case "g":
                            dashPattern.Add(ReadAnimatableFloat(dashObj?.ObjectPropertyOrNull("v")).InitialValue);
                            break;
                    }
                }
            }

            obj.AssertAllPropertiesRead();
            return new SolidColorStroke(
                in shapeLayerContentArgs,
                offset ?? s_animatableDoubleZero,
                dashPattern,
                color,
                opacity,
                strokeWidth,
                capType,
                joinType,
                miterLimit);
        }

        // gs
        ShapeLayerContent ReadGradientStroke(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            switch (TToGradientType(obj.DoublePropertyOrNull("t")))
            {
                case GradientType.Linear:
                    return ReadLinearGradientStroke(obj, in shapeLayerContentArgs);
                case GradientType.Radial:
                    return ReadRadialGradientStroke(obj, in shapeLayerContentArgs);
                default:
                    throw Unreachable;
            }
        }

        LinearGradientStroke ReadLinearGradientStroke(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("hd", "t", "1");

            var opacity = ReadAnimatableOpacity(obj.ObjectPropertyOrNull("o"));
            var strokeWidth = ReadAnimatableFloat(obj.ObjectPropertyOrNull("w"));
            var capType = LcToLineCapType(obj.DoublePropertyOrNull("lc"));
            var joinType = LjToLineJoinType(obj.DoublePropertyOrNull("lj"));
            var miterLimit = obj.DoublePropertyOrNull("ml") ?? 4; // Default miter limit in After Effects is 4
            var startPoint = ReadAnimatableVector3(obj.ObjectPropertyOrNull("s"));
            var endPoint = ReadAnimatableVector3(obj.ObjectPropertyOrNull("e"));
            var gradientStops = ReadAnimatableGradientStops(obj.ObjectPropertyOrNull("g"));

            obj.AssertAllPropertiesRead();
            return new LinearGradientStroke(
                in shapeLayerContentArgs,
                opacity,
                strokeWidth,
                capType,
                joinType,
                miterLimit,
                startPoint,
                endPoint,
                gradientStops);
        }

        RadialGradientStroke ReadRadialGradientStroke(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("t", "h", "1");

            var highlightLengthObject = obj.ObjectPropertyOrNull("h");
            var highlightLength = ReadAnimatableFloat(highlightLengthObject);

            var highlightAngleObject = obj.ObjectPropertyOrNull("a");
            var highlightDegrees = ReadAnimatableFloat(highlightAngleObject);

            var opacity = ReadAnimatableOpacity(obj.ObjectPropertyOrNull("o"));
            var strokeWidth = ReadAnimatableFloat(obj.ObjectPropertyOrNull("w"));
            var capType = LcToLineCapType(obj.DoublePropertyOrNull("lc"));
            var joinType = LjToLineJoinType(obj.DoublePropertyOrNull("lj"));
            var miterLimit = obj.DoublePropertyOrNull("ml") ?? 4; // Default miter limit in After Effects is 4
            var startPoint = ReadAnimatableVector3(obj.ObjectPropertyOrNull("s"));
            var endPoint = ReadAnimatableVector3(obj.ObjectPropertyOrNull("e"));
            var gradientStops = ReadAnimatableGradientStops(obj.ObjectPropertyOrNull("g"));

            obj.AssertAllPropertiesRead();
            return new RadialGradientStroke(
                in shapeLayerContentArgs,
                opacity: opacity,
                strokeWidth: strokeWidth,
                capType: capType,
                joinType: joinType,
                miterLimit: miterLimit,
                startPoint: startPoint,
                endPoint: endPoint,
                gradientStops: gradientStops,
                highlightLength: highlightLength,
                highlightDegrees: highlightDegrees);
        }

        // "fl"
        SolidColorFill ReadSolidColorFill(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("fillEnabled", "cl", "hd");

            var fillType = ReadFillType(obj);
            var opacity = ReadAnimatableOpacity(obj.ObjectPropertyOrNull("o"));
            var color = ReadAnimatableColor(obj.ObjectPropertyOrNull("c"));

            obj.AssertAllPropertiesRead();
            return new SolidColorFill(in shapeLayerContentArgs, fillType, opacity, color);
        }

        // gf
        ShapeLayerContent ReadGradientFill(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            switch (TToGradientType(obj.DoublePropertyOrNull("t")))
            {
                case GradientType.Linear:
                    return ReadLinearGradientFill(obj, in shapeLayerContentArgs);
                case GradientType.Radial:
                    return ReadRadialGradientFill(obj, in shapeLayerContentArgs);
                default:
                    throw Unreachable;
            }
        }

        RadialGradientFill ReadRadialGradientFill(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("hd", "1");

            var fillType = ReadFillType(obj);
            var opacity = ReadAnimatableOpacity(obj.ObjectPropertyOrNull("o"));
            var startPoint = ReadAnimatableVector3(obj.ObjectPropertyOrNull("s"));
            var endPoint = ReadAnimatableVector3(obj.ObjectPropertyOrNull("e"));
            var gradientStops = ReadAnimatableGradientStops(obj.ObjectPropertyOrNull("g"));

            var highlightLengthObject = obj.ObjectPropertyOrNull("h");
            var highlightLength = ReadAnimatableFloat(highlightLengthObject);

            var highlightAngleObject = obj.ObjectPropertyOrNull("a");
            var highlightDegrees = ReadAnimatableFloat(highlightAngleObject);

            obj.AssertAllPropertiesRead();
            return new RadialGradientFill(
                in shapeLayerContentArgs,
                fillType: fillType,
                opacity: opacity,
                startPoint: startPoint,
                endPoint: endPoint,
                gradientStops: gradientStops,
                highlightLength: highlightLength,
                highlightDegrees: highlightDegrees);
        }

        LinearGradientFill ReadLinearGradientFill(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("hd");

            var fillType = ReadFillType(obj);
            var opacity = ReadAnimatableOpacity(obj.ObjectPropertyOrNull("o"));
            var startPoint = ReadAnimatableVector3(obj.ObjectPropertyOrNull("s"));
            var endPoint = ReadAnimatableVector3(obj.ObjectPropertyOrNull("e"));
            var gradientStops = ReadAnimatableGradientStops(obj.ObjectPropertyOrNull("g"));

            obj.AssertAllPropertiesRead();
            return new LinearGradientFill(
                in shapeLayerContentArgs,
                fillType: fillType,
                opacity: opacity,
                startPoint: startPoint,
                endPoint: endPoint,
                gradientStops: gradientStops);
        }

        Ellipse ReadEllipse(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("closed", "hd");

            var position = ReadAnimatableVector3(obj.ObjectPropertyOrNull("p"));
            var diameter = ReadAnimatableVector3(obj.ObjectPropertyOrNull("s"));
            var direction = obj.BoolPropertyOrNull("d") == true;
            obj.AssertAllPropertiesRead();
            return new Ellipse(in shapeLayerContentArgs, direction, position, diameter);
        }

        Polystar ReadPolystar(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("ix");

            var direction = obj.BoolPropertyOrNull("d") == true;

            var points = ReadAnimatableFloat(obj.ObjectPropertyOrNull("pt"));
            var position = ReadAnimatableVector3(obj.ObjectPropertyOrNull("p"));
            var rotation = ReadAnimatableFloat(obj.ObjectPropertyOrNull("r"));
            var outerRadius = ReadAnimatableFloat(obj.ObjectPropertyOrNull("or"));
            var outerRoundedness = ReadAnimatableFloat(obj.ObjectPropertyOrNull("os"));

            var polystarType = SyToPolystarType(obj.DoublePropertyOrNull("sy")) ?? Polystar.PolyStarType.Polygon;

            Animatable<double> innerRadius;
            Animatable<double> innerRoundedness;

            switch (polystarType)
            {
                case Polystar.PolyStarType.Star:
                    innerRadius = ReadAnimatableFloat(obj.ObjectPropertyOrNull("ir"));
                    innerRoundedness = ReadAnimatableFloat(obj.ObjectPropertyOrNull("is"));
                    break;

                default:
                    innerRadius = null;
                    innerRoundedness = null;
                    break;
            }

            obj.AssertAllPropertiesRead();
            return new Polystar(
                in shapeLayerContentArgs,
                direction,
                polystarType,
                points,
                position,
                rotation,
                innerRadius,
                outerRadius,
                innerRoundedness,
                outerRoundedness);
        }

        Rectangle ReadRectangle(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("hd");

            var direction = obj.BoolPropertyOrNull("d") == true;
            var position = ReadAnimatableVector3(obj.ObjectPropertyOrNull("p"));
            var size = ReadAnimatableVector3(obj.ObjectPropertyOrNull("s"));
            var cornerRadius = ReadAnimatableFloat(obj.ObjectPropertyOrNull("r"));

            obj.AssertAllPropertiesRead();
            return new Rectangle(in shapeLayerContentArgs, direction, position, size, cornerRadius);
        }

        Path ReadPath(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("ind", "ix", "hd", "cl", "closed");

            var geometry = ReadAnimatableGeometry(obj.ObjectPropertyOrNull("ks"));
            var direction = obj.BoolPropertyOrNull("d") == true;
            obj.AssertAllPropertiesRead();
            return new Path(in shapeLayerContentArgs, direction, geometry);
        }

        TrimPath ReadTrimPath(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("ix", "hd");

            var startTrim = ReadAnimatableTrim(obj.ObjectPropertyOrNull("s"));
            var endTrim = ReadAnimatableTrim(obj.ObjectPropertyOrNull("e"));
            var offset = ReadAnimatableRotation(obj.ObjectPropertyOrNull("o"));
            var trimType = MToTrimType(obj.DoublePropertyOrNull("m"));
            obj.AssertAllPropertiesRead();
            return new TrimPath(
                in shapeLayerContentArgs,
                trimType,
                startTrim,
                endTrim,
                offset);
        }

        Repeater ReadRepeater(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            var count = ReadAnimatableFloat(obj.ObjectPropertyOrNull("c"));
            var offset = ReadAnimatableFloat(obj.ObjectPropertyOrNull("o"));
            var transform = ReadRepeaterTransform(obj.ObjectPropertyOrNull("tr"), in shapeLayerContentArgs);
            return new Repeater(in shapeLayerContentArgs, count, offset, transform);
        }

        MergePaths ReadMergePaths(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("hd");

            var mergeMode = MmToMergeMode(obj.DoublePropertyOrNull("mm"));
            obj.AssertAllPropertiesRead();
            return new MergePaths(
                in shapeLayerContentArgs,
                mergeMode);
        }

        RoundedCorner ReadRoundedCorner(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("hd", "ix");

            var radius = ReadAnimatableFloat(obj.ObjectPropertyOrNull("r"));
            obj.AssertAllPropertiesRead();
            return new RoundedCorner(
                in shapeLayerContentArgs,
                radius);
        }

        ShapeFill.PathFillType ReadFillType(in LottieJsonObjectElement obj)
        {
            // If not specified, the fill type is EvenOdd.
            var windingValue = obj.Int32PropertyOrNull("r");
            switch (windingValue)
            {
                case 0: return ShapeFill.PathFillType.EvenOdd;
                case 1: return ShapeFill.PathFillType.Winding;

                // TODO - some files have a "2" value. There
                // may be another fill type.
                default: return ShapeFill.PathFillType.EvenOdd;
            }
        }

        // Reads the transform for a repeater. Repeater transforms are the same as regular transforms
        // except they have an extra couple properties.
        RepeaterTransform ReadRepeaterTransform(
            in LottieJsonObjectElement? obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            if (obj is null)
            {
                return new RepeaterTransform(
                    shapeLayerContentArgs,
                    s_animatableVector3Zero,
                    s_animatableVector3Zero,
                    s_animatableVector3Zero,
                    s_animatableRotationNone,
                    s_animatableOpacityOpaque,
                    s_animatableOpacityOpaque,
                    s_animatableOpacityOpaque);
            }
            else
            {
                return ReadRepeaterTransform(obj.Value, shapeLayerContentArgs);
            }
        }

        RepeaterTransform ReadRepeaterTransform(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            obj.IgnorePropertyThatIsNotYetSupported("nm", "ty");

            var startOpacity = ReadAnimatableOpacity(obj.ObjectPropertyOrNull("so"));
            var endOpacity = ReadAnimatableOpacity(obj.ObjectPropertyOrNull("eo"));
            var transform = ReadTransform(obj, in shapeLayerContentArgs);

            obj.AssertAllPropertiesRead();
            return new RepeaterTransform(
                in shapeLayerContentArgs,
                transform.Anchor,
                transform.Position,
                transform.ScalePercent,
                transform.Rotation,
                transform.Opacity,
                startOpacity,
                endOpacity);
        }

        Transform ReadTransform(
            in LottieJsonObjectElement? obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            if (obj is null)
            {
                return new Transform(
                    shapeLayerContentArgs,
                    s_animatableVector3Zero,
                    s_animatableVector3Zero,
                    s_animatableVector3Zero,
                    s_animatableRotationNone,
                    s_animatableOpacityOpaque);
            }
            else
            {
                return ReadTransform(obj.Value, shapeLayerContentArgs);
            }
        }

        Transform ReadTransform(
            in LottieJsonObjectElement obj,
            in ShapeLayerContent.ShapeLayerContentArgs shapeLayerContentArgs)
        {
            // Not clear whether we need to read these properties.
            obj.IgnorePropertyThatIsNotYetSupported("sa", "sk");
            obj.IgnorePropertyThatIsNotYetSupported("or", "rx", "ry");
            obj.IgnorePropertyThatIsNotYetSupported("nm", "ty");

            var anchorJson = obj.ObjectPropertyOrNull("a");

            var anchor = ReadAnimatableVector3(anchorJson);
            var positionJson = obj.ObjectPropertyOrNull("p");
            var position = ReadAnimatableVector3(positionJson);
            var scaleJson = obj.ObjectPropertyOrNull("s");
            var scalePercent = ReadAnimatableVector3(scaleJson, s_animatableVector3OneHundred);
            var rotation = ReadAnimatableRotation(obj.ObjectPropertyOrNull("r") ?? obj.ObjectPropertyOrNull("rz"));

            var opacity = ReadAnimatableOpacity(obj.ObjectPropertyOrNull("o"));

            obj.AssertAllPropertiesRead();
            return new Transform(in shapeLayerContentArgs, anchor, position, scalePercent, rotation, opacity);
        }
    }
}