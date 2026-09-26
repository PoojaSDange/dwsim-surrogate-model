using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DWSIM.Automation;


class Program
{
    // =============================================================
    // MAIN CONFIGURATION
    // =============================================================

    // Number of DWSIM cases we want
    const int NUMBER_OF_CASES = 3000;

    // Random seed.
    // Keeping this fixed means the same 3000 samples are generated
    // every time you run the program.
    const int RANDOM_SEED = 42;


    static void Main()
    {
        Console.WriteLine("======================================================");
        Console.WriteLine("   DWSIM SURROGATE MODEL DATASET GENERATOR");
        Console.WriteLine("   3000 Latin Hypercube Samples");
        Console.WriteLine("======================================================");


        // =========================================================
        // 1. DWSIM PATHS
        // =========================================================

        string dwsimPath =
            Environment.GetEnvironmentVariable("DWSIM_PATH");

        if (string.IsNullOrWhiteSpace(dwsimPath))
        {
            throw new Exception(
                "DWSIM_PATH environment variable is not set. " +
                "Please set it to your DWSIM installation directory."
            );
        }

        string thermoPath =
            Path.Combine(dwsimPath, "ThermoCS");

        // --- Project root resolution ---
        // AppContext.BaseDirectory = .../DataGeneration/bin/Debug/net<version>/
        // Walk up from there to DWSIM_Surrogate_Model/ (the project root)
        string projectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")
        );

        string flowsheetPath =
            Path.Combine(projectRoot, "simulation.dwxmz");

        string csvPath =
            Path.Combine(projectRoot, "dwsim_dataset.csv");


        Environment.CurrentDirectory = dwsimPath;


        string oldPath =
            Environment.GetEnvironmentVariable("PATH");

        Environment.SetEnvironmentVariable(
            "PATH",
            dwsimPath + ";" + thermoPath + ";" + oldPath
        );


        // =========================================================
        // 2. THERMOCS DLL RESOLUTION
        // =========================================================

        AppDomain.CurrentDomain.AssemblyResolve +=
            (sender, args) =>
            {
                AssemblyName requestedAssembly =
                    new AssemblyName(args.Name);

                string assemblyName =
                    requestedAssembly.Name + ".dll";

                // Search main DWSIM folder
                string dllPath =
                    Path.Combine(
                        dwsimPath,
                        assemblyName
                    );

                if (File.Exists(dllPath))
                {
                    return Assembly.LoadFrom(dllPath);
                }

                // Search ThermoCS folder
                dllPath =
                    Path.Combine(
                        thermoPath,
                        assemblyName
                    );

                if (File.Exists(dllPath))
                {
                    return Assembly.LoadFrom(dllPath);
                }

                return null;
            };


        Console.WriteLine();
        Console.WriteLine("Flowsheet:");
        Console.WriteLine(flowsheetPath);

        Console.WriteLine();
        Console.WriteLine("CSV output:");
        Console.WriteLine(csvPath);


        // =========================================================
        // 3. OPERATING RANGES
        // =========================================================
        //
        // These are the ranges from which we generate samples.
        //
        // REQUIRED INPUTS:
        //
        // 1. Feed temperature
        // 2. Feed pressure
        // 3. Feed composition
        // 4. Number of stages
        // 5. Feed stage
        // 6. Reflux ratio
        // 7. Bottoms withdrawal rate
        //
        // ADDITIONAL INPUT:
        //
        // 8. Feed flow
        //
        // =========================================================


        // Feed temperature [K]
        double feedTemperatureMin = 350.0;
        double feedTemperatureMax = 380.0;


        // Feed pressure [Pa]
        // 1 atm -> 101325 Pa
        // 2 atm -> 202650 Pa
        double feedPressureMin = 101325.0;
        double feedPressureMax = 202650.0;


        // Benzene mole fraction in feed
        double feedCompositionMin = 0.30;
        double feedCompositionMax = 0.70;


        // Reflux ratio
        double refluxRatioMin = 1.5;
        double refluxRatioMax = 4.0;


        // Bottoms withdrawal rate [mol/s]
        double bottomsFlowMin = 8.0;
        double bottomsFlowMax = 17.0;


        // Additional feature: feed flow [mol/s]
        double feedFlowMin = 20.0;
        double feedFlowMax = 35.0;


        // Number of stages
        int numberOfStagesMin = 15;
        int numberOfStagesMax = 21;


        // =========================================================
        // 4. DISPLAY DATASET CONFIGURATION
        // =========================================================

        Console.WriteLine();
        Console.WriteLine("======================================================");
        Console.WriteLine("DATASET CONFIGURATION");
        Console.WriteLine("======================================================");

        Console.WriteLine(
            "Number of cases       = " +
            NUMBER_OF_CASES
        );

        Console.WriteLine(
            "Feed temperature      = " +
            feedTemperatureMin + " - " +
            feedTemperatureMax + " K"
        );

        Console.WriteLine(
            "Feed pressure         = " +
            feedPressureMin + " - " +
            feedPressureMax + " Pa"
        );

        Console.WriteLine(
            "Feed composition      = " +
            feedCompositionMin + " - " +
            feedCompositionMax
        );

        Console.WriteLine(
            "Number of stages      = " +
            numberOfStagesMin + " - " +
            numberOfStagesMax
        );

        Console.WriteLine(
            "Feed stage            = valid stage inside column"
        );

        Console.WriteLine(
            "Reflux ratio          = " +
            refluxRatioMin + " - " +
            refluxRatioMax
        );

        Console.WriteLine(
            "Bottoms flow          = " +
            bottomsFlowMin + " - " +
            bottomsFlowMax + " mol/s"
        );

        Console.WriteLine(
            "Feed flow             = " +
            feedFlowMin + " - " +
            feedFlowMax + " mol/s"
        );


        // =========================================================
        // 5. RANDOM GENERATOR
        // =========================================================

        Random random =
            new Random(RANDOM_SEED);


        // =========================================================
        // 6. GENERATE LATIN HYPERCUBE SAMPLES
        // =========================================================
        //
        // Each continuous variable is divided into 3000 strata.
        //
        // Therefore the samples cover the entire operating range
        // much more evenly than simple random sampling.
        // =========================================================

        double[] feedTemperatures =
            LatinHypercube(
                NUMBER_OF_CASES,
                feedTemperatureMin,
                feedTemperatureMax,
                random
            );


        double[] feedPressures =
            LatinHypercube(
                NUMBER_OF_CASES,
                feedPressureMin,
                feedPressureMax,
                random
            );


        double[] feedCompositions =
            LatinHypercube(
                NUMBER_OF_CASES,
                feedCompositionMin,
                feedCompositionMax,
                random
            );


        double[] refluxRatios =
            LatinHypercube(
                NUMBER_OF_CASES,
                refluxRatioMin,
                refluxRatioMax,
                random
            );


        double[] bottomsFlows =
            LatinHypercube(
                NUMBER_OF_CASES,
                bottomsFlowMin,
                bottomsFlowMax,
                random
            );


        double[] feedFlows =
            LatinHypercube(
                NUMBER_OF_CASES,
                feedFlowMin,
                feedFlowMax,
                random
            );


        // =========================================================
        // 7. GENERATE NUMBER OF STAGES
        // =========================================================
        //
        // Number of stages is discrete, so we cannot use the same
        // continuous sampling directly.
        //
        // We generate a shuffled sequence of stage values.
        // =========================================================

        int[] numberOfStages =
            GenerateDiscreteLHS(
                NUMBER_OF_CASES,
                numberOfStagesMin,
                numberOfStagesMax,
                random
            );


        // =========================================================
        // 8. GENERATE FEED STAGE
        // =========================================================
        //
        // Feed stage depends on number of stages.
        //
        // IMPORTANT:
        //
        // feedStage <= numberOfStages
        //
        // We choose a valid internal stage from 2 to N-1.
        // =========================================================

        int[] feedStages =
            new int[NUMBER_OF_CASES];


        for (int i = 0; i < NUMBER_OF_CASES; i++)
        {
            int N = numberOfStages[i];

            // Keep feed stage inside the column.
            //
            // Example:
            // N = 18
            // possible feed stages = 2 ... 17

            int minimumFeedStage = 2;
            int maximumFeedStage = N - 1;


            feedStages[i] =
                random.Next(
                    minimumFeedStage,
                    maximumFeedStage + 1
                );
        }


        // =========================================================
        // 9. CREATE DWSIM AUTOMATION
        // =========================================================

        Console.WriteLine();
        Console.WriteLine("Creating Automation3...");

        Automation3 manager =
            new Automation3();

        Console.WriteLine(
            "DWSIM Automation initialized!"
        );


        // =========================================================
        // 10. CREATE CSV DIRECTORY
        // =========================================================

        string csvDirectory =
            Path.GetDirectoryName(csvPath);

        if (!string.IsNullOrEmpty(csvDirectory))
        {
            Directory.CreateDirectory(csvDirectory);
        }


        // =========================================================
        // 11. DIAGNOSTIC FLAGS
        // =========================================================

        bool printedSpecKeysOnce = false;
        bool printedDutyDiscoveryOnce = false;
        bool printedColumnPropertiesOnce = false;


        // =========================================================
        // 12. CREATE CSV
        // =========================================================

        using (StreamWriter writer =
               new StreamWriter(csvPath, false))
        {

            // =====================================================
            // CSV HEADER
            // =====================================================
            //
            // FIRST 8 COLUMNS = INPUT FEATURES
            //
            // LAST 4 COLUMNS = TARGETS
            //
            // =====================================================

            writer.WriteLine(
                "feed_temperature," +
                "feed_pressure," +
                "feed_composition," +
                "number_of_stages," +
                "feed_stage," +
                "reflux_ratio," +
                "bottoms_flow," +
                "feed_flow," +
                "xD," +
                "xB," +
                "QC," +
                "QR"
            );


            int successfulRuns = 0;
            int failedRuns = 0;


            // =====================================================
            // 13. RUN THE 3000 CASES
            // =====================================================

            for (int i = 0;
                 i < NUMBER_OF_CASES;
                 i++)
            {

                double feedTemperature =
                    feedTemperatures[i];

                double feedPressure =
                    feedPressures[i];

                double feedComp0 =
                    feedCompositions[i];

                double feedComp1 =
                    1.0 - feedComp0;

                double refluxRatio =
                    refluxRatios[i];

                double bottomsFlow =
                    bottomsFlows[i];

                double feedFlow =
                    feedFlows[i];

                int stages =
                    numberOfStages[i];

                int feedStage =
                    feedStages[i];


                // =================================================
                // DISPLAY CURRENT CASE
                // =================================================

                Console.WriteLine();
                Console.WriteLine(
                    "======================================================"
                );

                Console.WriteLine(
                    "CASE " +
                    (i + 1) +
                    "/" +
                    NUMBER_OF_CASES
                );

                Console.WriteLine(
                    "Feed flow        = " +
                    feedFlow +
                    " mol/s"
                );

                Console.WriteLine(
                    "Feed temperature = " +
                    feedTemperature +
                    " K"
                );

                Console.WriteLine(
                    "Feed pressure    = " +
                    feedPressure +
                    " Pa"
                );

                Console.WriteLine(
                    "Feed composition = [" +
                    feedComp0 +
                    ", " +
                    feedComp1 +
                    "]"
                );

                Console.WriteLine(
                    "Stages           = " +
                    stages
                );

                Console.WriteLine(
                    "Feed stage       = " +
                    feedStage
                );

                Console.WriteLine(
                    "Reflux ratio     = " +
                    refluxRatio
                );

                Console.WriteLine(
                    "Bottoms flow     = " +
                    bottomsFlow +
                    " mol/s"
                );


                try
                {

                    // =================================================
                    // 14. LOAD FRESH FLOWSHEET
                    // =================================================

                    var sim =
                        manager.LoadFlowsheet(
                            flowsheetPath
                        );


                    // =================================================
                    // 15. FIND FEED STREAM
                    // =================================================

                    object feed =
                        FindSimulationObjectByTag(
                            sim,
                            "Feed Pipe"
                        );

                    if (feed == null)
                    {
                        throw new Exception(
                            "Feed Pipe was not found."
                        );
                    }


                    // =================================================
                    // 16. FIND DISTILLATION COLUMN
                    // =================================================

                    object column =
                        FindSimulationObjectByTag(
                            sim,
                            "DCOL-1"
                        );

                    if (column == null)
                    {
                        throw new Exception(
                            "DCOL-1 was not found."
                        );
                    }


                    // =================================================
                    // 17. PRINT COLUMN API INFORMATION ONCE
                    // =================================================
                    //
                    // This is important because the exact DWSIM
                    // property names for stage configuration can
                    // differ between versions.
                    // =================================================

                    if (!printedColumnPropertiesOnce)
                    {
                        PrintColumnProperties(column);

                        printedColumnPropertiesOnce = true;
                    }


                    // =================================================
                    // 18. SET FEED CONDITIONS
                    // =================================================

                    InvokeSetter(
                        feed,
                        "SetMolarFlow",
                        feedFlow
                    );

                    InvokeSetter(
                        feed,
                        "SetTemperature",
                        feedTemperature
                    );

                    InvokeSetter(
                        feed,
                        "SetPressure",
                        feedPressure
                    );


                    // =================================================
                    // 19. SET FEED COMPOSITION
                    // =================================================

                    var setCompositionMethod =
                        feed.GetType().GetMethod(
                            "SetOverallMolarComposition"
                        );

                    if (setCompositionMethod == null)
                    {
                        throw new Exception(
                            "SetOverallMolarComposition(double[]) " +
                            "was not found."
                        );
                    }

                    setCompositionMethod.Invoke(
                        feed,
                        new object[]
                        {
                            new double[]
                            {
                                feedComp0,
                                feedComp1
                            }
                        }
                    );


                    // =================================================
                    // 20. SET NUMBER OF STAGES
                    // =================================================

                    SetColumnStages(column, stages);




                    // =================================================
                    // 21. SET FEED STAGE
                    // =================================================

                    SetColumnFeedStage(column, feed, feedStage);


                    // =================================================
                    // 22. ACCESS COLUMN SPECS
                    // =================================================

                    var specsProperty =
                        column.GetType().GetProperty(
                            "Specs"
                        );

                    if (specsProperty == null)
                    {
                        throw new Exception(
                            "Column Specs property was not found."
                        );
                    }

                    var specs =
                        specsProperty.GetValue(column);

                    if (specs == null)
                    {
                        throw new Exception(
                            "Column Specs is null."
                        );
                    }

                    var dictionary =
                        specs as IDictionary;

                    if (dictionary == null)
                    {
                        throw new Exception(
                            "Column Specs could not be accessed " +
                            "as IDictionary."
                        );
                    }


                    // =================================================
                    // 23. PRINT COLUMN SPEC KEYS ONCE
                    // =================================================

                    if (!printedSpecKeysOnce)
                    {
                        Console.WriteLine();
                        Console.WriteLine(
                            ">>> AVAILABLE COLUMN SPEC KEYS:"
                        );

                        foreach (var key in dictionary.Keys)
                        {
                            Console.WriteLine(
                                "    - '" +
                                key +
                                "'"
                            );
                        }

                        printedSpecKeysOnce = true;
                    }


                    // =================================================
                    // 24. SET BOTTOMS WITHDRAWAL
                    // =================================================
                    //
                    // Your previous testing established that "R"
                    // controls the bottoms flow in this flowsheet.
                    // =================================================

                    SetSpecValue(
                        dictionary,
                        "R",
                        bottomsFlow,
                        "bottoms flow"
                    );


                    // =================================================
                    // 25. SET REFLUX RATIO
                    // =================================================
                    //
                    // Your existing code uses "C".
                    // =================================================

                    string[] refluxKeyCandidates =
                    {
                        "C"
                    };

                    string usedRefluxKey =
                        SetSpecValueFlexible(
                            dictionary,
                            refluxKeyCandidates,
                            refluxRatio,
                            "reflux ratio"
                        );


                    // =================================================
                    // 26. CALCULATE FLOWSHEET
                    // =================================================

                    var calculateMethod =
                        manager.GetType().GetMethod(
                            "CalculateFlowsheet4"
                        );

                    if (calculateMethod == null)
                    {
                        throw new Exception(
                            "CalculateFlowsheet4 was not found."
                        );
                    }

                    object calculationResult =
                        calculateMethod.Invoke(
                            manager,
                            new object[]
                            {
                                sim
                            }
                        );


                    // =================================================
                    // 27. CHECK DWSIM ERRORS
                    // =================================================

                    int errorCount = 0;

                    var errors =
                        calculationResult as IEnumerable;

                    if (errors != null)
                    {
                        foreach (object error in errors)
                        {
                            errorCount++;

                            Console.WriteLine(
                                "DWSIM ERROR: " +
                                error
                            );
                        }
                    }

                    if (errorCount > 0)
                    {
                        throw new Exception(
                            "DWSIM calculation returned " +
                            errorCount +
                            " error(s)."
                        );
                    }


                    Console.WriteLine(
                        "Calculation successful."
                    );


                    // =================================================
                    // 28. FIND PRODUCT STREAMS
                    // =================================================

                    object distillate =
                        FindSimulationObjectByTag(
                            sim,
                            "3"
                        );

                    object bottoms =
                        FindSimulationObjectByTag(
                            sim,
                            "4"
                        );


                    // =================================================
                    // 29. FIND ENERGY STREAMS
                    // =================================================

                    object condenserEnergy =
                        FindSimulationObjectByTag(
                            sim,
                            "E1"
                        );

                    object reboilerEnergy =
                        FindSimulationObjectByTag(
                            sim,
                            "E2"
                        );


                    if (distillate == null)
                    {
                        throw new Exception(
                            "Distillate stream '3' was not found."
                        );
                    }

                    if (bottoms == null)
                    {
                        throw new Exception(
                            "Bottoms stream '4' was not found."
                        );
                    }

                    if (condenserEnergy == null)
                    {
                        throw new Exception(
                            "Condenser energy stream 'E1' " +
                            "was not found."
                        );
                    }

                    if (reboilerEnergy == null)
                    {
                        throw new Exception(
                            "Reboiler energy stream 'E2' " +
                            "was not found."
                        );
                    }


                    // =================================================
                    // 30. READ COMPOSITIONS
                    // =================================================

                    double[] distillateComposition =
                        GetComposition(
                            distillate
                        );

                    double[] bottomsComposition =
                        GetComposition(
                            bottoms
                        );


                    if (distillateComposition == null ||
                        distillateComposition.Length < 2)
                    {
                        throw new Exception(
                            "Distillate composition is invalid."
                        );
                    }

                    if (bottomsComposition == null ||
                        bottomsComposition.Length < 2)
                    {
                        throw new Exception(
                            "Bottoms composition is invalid."
                        );
                    }


                    // =================================================
                    // 31. DEFINE xD AND xB
                    // =================================================
                    //
                    // Assuming:
                    //
                    // component 0 = Benzene
                    // component 1 = Toluene
                    //
                    // Benzene is the light component.
                    //
                    // Therefore:
                    //
                    // xD = benzene purity in distillate
                    // xB = toluene purity in bottoms
                    // =================================================

                    double xD =
                        distillateComposition[0];

                    double xB =
                        bottomsComposition[1];


                    // =================================================
                    // 32. READ CONDENSER DUTY
                    // =================================================

                    (double condenserDuty,
                     string condenserAccessor)
                        =
                        GetDoubleFlexible(
                            condenserEnergy,
                            new (string, string)[]
                            {
                                ("EnergyFlow", "GetEnergyFlow"),
                                ("Duty", "GetDuty"),
                                ("HeatDuty", "GetHeatDuty")
                            }
                        );


                    // =================================================
                    // 33. READ REBOILER DUTY
                    // =================================================

                    (double reboilerDuty,
                     string reboilerAccessor)
                        =
                        GetDoubleFlexible(
                            reboilerEnergy,
                            new (string, string)[]
                            {
                                ("EnergyFlow", "GetEnergyFlow"),
                                ("Duty", "GetDuty"),
                                ("HeatDuty", "GetHeatDuty")
                            }
                        );


                    // =================================================
                    // 34. CONVERT DUTIES TO POSITIVE MAGNITUDES
                    // =================================================
                    //
                    // We want:
                    //
                    // QC = magnitude of condenser duty
                    // QR = magnitude of reboiler duty
                    //
                    // Example:
                    //
                    // DWSIM: condenser = -500000
                    //
                    // Dataset: QC = 500000
                    // =================================================

                    double QC =condenserDuty;

                    double QR =reboilerDuty;


                    if (!printedDutyDiscoveryOnce)
                    {
                        Console.WriteLine(
                            ">>> Duty read via: " +
                            "condenser='" +
                            condenserAccessor +
                            "', reboiler='" +
                            reboilerAccessor +
                            "'"
                        );

                        printedDutyDiscoveryOnce = true;
                    }


                    // =================================================
                    // 35. BASIC VALIDATION
                    // =================================================

                    if (double.IsNaN(xD) ||
                        double.IsInfinity(xD) ||
                        xD < 0.0 ||
                        xD > 1.0)
                    {
                        throw new Exception(
                            "Invalid xD value: " + xD
                        );
                    }


                    if (double.IsNaN(xB) ||
                        double.IsInfinity(xB) ||
                        xB < 0.0 ||
                        xB > 1.0)
                    {
                        throw new Exception(
                            "Invalid xB value: " + xB
                        );
                    }


                    if (double.IsNaN(QC) ||
                        double.IsInfinity(QC))
                    {
                        throw new Exception(
                            "Invalid QC value: " + QC
                        );
                    }


                    if (double.IsNaN(QR) ||
                        double.IsInfinity(QR))
                    {
                        throw new Exception(
                            "Invalid QR value: " + QR
                        );
                    }


                    // =================================================
                    // 36. PRINT RESULT
                    // =================================================

                    Console.WriteLine();
                    Console.WriteLine("RESULT:");

                    Console.WriteLine(
                        "  xD = " +
                        xD
                    );

                    Console.WriteLine(
                        "  xB = " +
                        xB
                    );

                    Console.WriteLine(
                        "  QC = " +
                        QC
                    );

                    Console.WriteLine(
                        "  QR = " +
                        QR
                    );


                    // =================================================
                    // 37. WRITE CSV ROW
                    // =================================================

                    writer.WriteLine(

                        CsvValue(feedTemperature) + "," +

                        CsvValue(feedPressure) + "," +

                        CsvValue(feedComp0) + "," +

                        stages.ToString(
                            CultureInfo.InvariantCulture
                        ) + "," +

                        feedStage.ToString(
                            CultureInfo.InvariantCulture
                        ) + "," +

                        CsvValue(refluxRatio) + "," +

                        CsvValue(bottomsFlow) + "," +

                        CsvValue(feedFlow) + "," +

                        CsvValue(xD) + "," +

                        CsvValue(xB) + "," +

                        CsvValue(QC) + "," +

                        CsvValue(QR)
                    );


                    writer.Flush();

                    successfulRuns++;
                }
                catch (Exception ex)
                {
                    failedRuns++;

                    Console.WriteLine();
                    Console.WriteLine(
                        "!!! CASE FAILED !!!"
                    );

                    Console.WriteLine(
                        ex.ToString()
                    );

                    // Continue to next case.
                }


                // =====================================================
                // PROGRESS
                // =====================================================

                Console.WriteLine();
                Console.WriteLine(
                    "Successful = " +
                    successfulRuns +
                    " | Failed = " +
                    failedRuns
                );
            }


            // =========================================================
            // 38. FINAL SUMMARY
            // =========================================================

            Console.WriteLine();
            Console.WriteLine(
                "======================================================"
            );

            Console.WriteLine(
                "DATASET GENERATION FINISHED"
            );

            Console.WriteLine(
                "======================================================"
            );

            Console.WriteLine(
                "Requested cases   = " +
                NUMBER_OF_CASES
            );

            Console.WriteLine(
                "Successful cases  = " +
                successfulRuns
            );

            Console.WriteLine(
                "Failed cases      = " +
                failedRuns
            );

            Console.WriteLine(
                "Success rate      = " +
                (
                    100.0 *
                    successfulRuns /
                    NUMBER_OF_CASES
                ).ToString(
                    "F2",
                    CultureInfo.InvariantCulture
                ) +
                "%"
            );

            Console.WriteLine(
                "CSV file          = " +
                csvPath
            );
        }


        // =========================================================
        // 39. RELEASE DWSIM
        // =========================================================

        manager.ReleaseResources();

        Console.WriteLine();
        Console.WriteLine(
            "DWSIM resources released."
        );

        Console.WriteLine(
            "Finished."
        );

        if (!Console.IsInputRedirected)
        {
            Console.ReadKey();

        }
    }


    // =============================================================
    // LATIN HYPERCUBE SAMPLING
    // =============================================================
    //
    // Generates N values between min and max.
    //
    // The interval is divided into N equal strata.
    // One random point is selected from every stratum.
    //
    // This gives better coverage than simple random sampling.
    // =============================================================

    static double[] LatinHypercube(
        int n,
        double min,
        double max,
        Random random)
    {
        double[] result =
            new double[n];

        int[] permutation =
            Enumerable
                .Range(0, n)
                .ToArray();


        // Shuffle permutation
        for (int i = n - 1; i > 0; i--)
        {
            int j =
                random.Next(i + 1);

            int temp =
                permutation[i];

            permutation[i] =
                permutation[j];

            permutation[j] =
                temp;
        }


        for (int i = 0; i < n; i++)
        {
            // Random point inside the stratum
            double u =
                (permutation[i] +
                 random.NextDouble()) /
                n;


            result[i] =
                min +
                u * (max - min);
        }


        return result;
    }


    // =============================================================
    // DISCRETE LATIN HYPERCUBE
    // =============================================================
    //
    // Used for integer variables such as number of stages.
    // =============================================================

    static int[] GenerateDiscreteLHS(
        int n,
        int min,
        int max,
        Random random)
    {
        int numberOfValues =
            max - min + 1;


        int[] result =
            new int[n];


        int[] permutation =
            Enumerable
                .Range(0, n)
                .ToArray();


        // Shuffle
        for (int i = n - 1; i > 0; i--)
        {
            int j =
                random.Next(i + 1);

            int temp =
                permutation[i];

            permutation[i] =
                permutation[j];

            permutation[j] =
                temp;
        }


        for (int i = 0; i < n; i++)
        {
            // Spread values approximately evenly
            int index =
                (int)(
                    ((double)permutation[i] /
                     n) *
                    numberOfValues
                );


            if (index >= numberOfValues)
            {
                index =
                    numberOfValues - 1;
            }


            result[i] =
                min + index;
        }


        return result;
    }


    // =============================================================
    // PRINT COLUMN PROPERTIES
    // =============================================================
    //
    // IMPORTANT:
    //
    // This tells us exactly how YOUR DWSIM version exposes
    // NumberOfStages and FeedStage.
    // =============================================================

    static void PrintColumnProperties(
        object column)
    {
        Type type =
            column.GetType();


        Console.WriteLine();
        Console.WriteLine(
            "======================================================"
        );

        Console.WriteLine(
            "DWSIM COLUMN DIAGNOSTIC"
        );

        Console.WriteLine(
            "======================================================"
        );

        Console.WriteLine(
            "Column type:"
        );

        Console.WriteLine(
            type.FullName
        );


        Console.WriteLine();
        Console.WriteLine(
            "COLUMN PROPERTIES:"
        );


        foreach (var property in type.GetProperties())
        {
            Console.WriteLine(
                "  " +
                property.Name +
                " : " +
                property.PropertyType.FullName +
                " | CanWrite=" +
                property.CanWrite
            );
        }


        Console.WriteLine();
        Console.WriteLine(
            "COLUMN METHODS:"
        );


        foreach (var method in type.GetMethods())
        {
            Console.WriteLine(
                "  " +
                method.Name
            );
        }


        Console.WriteLine(
            "======================================================"
        );
    }


    // =============================================================
    // SET NUMBER OF STAGES
    // =============================================================
    //
    // DWSIM VERSION-DEPENDENT SECTION.
    //
    // We try several common names.
    // =============================================================

    static void SetColumnStages(object column, int stages)
    {
        if (column == null)
            throw new Exception("Column is null.");

        if (stages < 2)
            throw new Exception("Number of stages must be at least 2.");

        Type columnType = column.GetType();

        // ---------------------------------------------------------
        // DWSIM provides SetNumberOfStages().
        // We use this instead of directly modifying NumberOfStages
        // because DWSIM must also rebuild/update its internal
        // Stages collection.
        // ---------------------------------------------------------

        var methods = columnType.GetMethods()
            .Where(m => m.Name == "SetNumberOfStages")
            .ToArray();

        if (methods.Length == 0)
            throw new Exception(
                "SetNumberOfStages() was not found on " +
                columnType.FullName
            );

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();

            if (parameters.Length != 1)
                continue;

            if (parameters[0].ParameterType != typeof(int))
                continue;

            try
            {
                method.Invoke(
                    column,
                    new object[] { stages }
                );

                Console.WriteLine(
                    "Number of stages set using: SetNumberOfStages"
                );

                // -------------------------------------------------
                // Verify the resulting NumberOfStages property
                // -------------------------------------------------

                var property = columnType.GetProperty("NumberOfStages");

                if (property != null)
                {
                    int actualStages = Convert.ToInt32(
                        property.GetValue(column),
                        CultureInfo.InvariantCulture
                    );

                    Console.WriteLine(
                        "Verified NumberOfStages = " +
                        actualStages
                    );

                    if (actualStages != stages)
                    {
                        throw new Exception(
                            "DWSIM NumberOfStages is " +
                            actualStages +
                            " after requesting " +
                            stages
                        );
                    }
                }

                // -------------------------------------------------
                // Verify internal Stages collection
                // -------------------------------------------------

                var stagesProperty = columnType.GetProperty("Stages");

                if (stagesProperty != null)
                {
                    object stagesObject =
                        stagesProperty.GetValue(column);

                    if (stagesObject is ICollection collection)
                    {
                        Console.WriteLine(
                            "Verified internal Stages collection count = " +
                            collection.Count
                        );

                        if (collection.Count != stages)
                        {
                            throw new Exception(
                                "Internal DWSIM Stages collection contains " +
                                collection.Count +
                                " stages, but " +
                                stages +
                                " were requested."
                            );
                        }
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "SetNumberOfStages() failed: " +
                    ex.Message,
                    ex
                );
            }
        }

        throw new Exception(
            "Could not find SetNumberOfStages(int) overload."
        );
    }


    // =============================================================
    // SET FEED STAGE
    // =============================================================
    //
    // DWSIM VERSION-DEPENDENT SECTION.
    //
    // We try several likely property names.
    // =============================================================
    static string GetStreamID(object stream)
    {
        if (stream == null)
            throw new Exception("Stream is null.");

        Type type = stream.GetType();

        // Try Name
        var nameProperty = type.GetProperty("Name");

        if (nameProperty != null)
        {
            object value = nameProperty.GetValue(stream);

            if (value != null)
                return value.ToString();
        }

        // Try GraphicObject Tag
        var graphicProperty = type.GetProperty("GraphicObject");

        if (graphicProperty != null)
        {
            object graphic = graphicProperty.GetValue(stream);

            if (graphic != null)
            {
                var tagProperty = graphic.GetType().GetProperty("Tag");

                if (tagProperty != null)
                {
                    object tag = tagProperty.GetValue(graphic);

                    if (tag != null)
                        return tag.ToString();
                }
            }
        }

        throw new Exception(
            "Could not determine the ID/name/tag of the feed stream."
        );
    }

    static void SetColumnFeedStage(object column, object feed, int feedStage)
    {
        if (column == null)
            throw new Exception("Column is null.");

        if (feed == null)
            throw new Exception("Feed stream is null.");

        Type columnType = column.GetType();

        // DWSIM 10.x uses SetStreamFeedStage()
        // instead of a FeedStage property on the column.

        var methods = columnType.GetMethods()
            .Where(m => m.Name == "SetStreamFeedStage")
            .ToArray();

        if (methods.Length == 0)
            throw new Exception(
                "SetStreamFeedStage method was not found on " +
                columnType.FullName
            );

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();

            Console.WriteLine(
                "Trying SetStreamFeedStage overload: " +
                method
            );

            try
            {
                // We need to determine which argument represents
                // the feed stream and which represents the stage.

                if (parameters.Length == 2)
                {
                    Type p0 = parameters[0].ParameterType;
                    Type p1 = parameters[1].ParameterType;

                    // Case:
                    // SetStreamFeedStage(string streamID, int stage)

                    if (p0 == typeof(string) &&
                        p1 == typeof(int))
                    {
                        string feedID = GetStreamID(feed);

                        method.Invoke(
                            column,
                            new object[] { feedID, feedStage }
                        );

                        Console.WriteLine(
                            "Feed stage set using stream ID = " +
                            feedID
                        );

                        return;
                    }

                    // Case:
                    // SetStreamFeedStage(int stage, string streamID)

                    if (p0 == typeof(int) &&
                        p1 == typeof(string))
                    {
                        string feedID = GetStreamID(feed);

                        method.Invoke(
                            column,
                            new object[] { feedStage, feedID }
                        );

                        Console.WriteLine(
                            "Feed stage set using stream ID = " +
                            feedID
                        );

                        return;
                    }

                    // Case where DWSIM expects the actual stream object
                    if (p0.IsAssignableFrom(feed.GetType()) &&
                        p1 == typeof(int))
                    {
                        method.Invoke(
                            column,
                            new object[] { feed, feedStage }
                        );

                        Console.WriteLine(
                            "Feed stage set using feed stream object."
                        );

                        return;
                    }

                    if (p0 == typeof(int) &&
                        p1.IsAssignableFrom(feed.GetType()))
                    {
                        method.Invoke(
                            column,
                            new object[] { feedStage, feed }
                        );

                        Console.WriteLine(
                            "Feed stage set using feed stream object."
                        );

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "This overload failed: " +
                    ex.Message
                );
            }
        }

        throw new Exception(
            "Could not determine the correct SetStreamFeedStage overload."
        );
    }


    // =============================================================
    // FIND SIMULATION OBJECT BY GRAPHIC TAG
    // =============================================================

    static object FindSimulationObjectByTag(
        dynamic sim,
        string requiredTag)
    {
        foreach (var obj in sim.SimulationObjects)
        {
            var graphicProperty =
                obj.Value
                   .GetType()
                   .GetProperty(
                       "GraphicObject"
                   );


            if (graphicProperty == null)
                continue;


            var graphicObject =
                graphicProperty.GetValue(
                    obj.Value
                );


            if (graphicObject == null)
                continue;


            var tagProperty =
                graphicObject
                    .GetType()
                    .GetProperty(
                        "Tag"
                    );


            if (tagProperty == null)
                continue;


            string tag =
                tagProperty
                    .GetValue(
                        graphicObject
                    )
                    ?.ToString();


            if (tag == requiredTag)
                return obj.Value;
        }


        return null;
    }


    // =============================================================
    // INVOKE DOUBLE SETTER
    // =============================================================

    static void InvokeSetter(
        object obj,
        string methodName,
        double value)
    {
        var method =
            obj.GetType().GetMethod(
                methodName,
                new Type[]
                {
                    typeof(double)
                }
            );


        if (method == null)
        {
            throw new Exception(
                methodName +
                "(double) was not found."
            );
        }


        method.Invoke(
            obj,
            new object[]
            {
                value
            }
        );
    }


    // =============================================================
    // SET SPEC VALUE
    // =============================================================

    static void SetSpecValue(
        IDictionary dictionary,
        string key,
        double value,
        string label)
    {
        if (!dictionary.Contains(key))
        {
            throw new Exception(
                label +
                " specification key '" +
                key +
                "' was not found."
            );
        }


        object spec =
            dictionary[key];


        if (spec == null)
        {
            throw new Exception(
                label +
                " specification is null."
            );
        }


        var specValueProperty =
            spec.GetType().GetProperty(
                "SpecValue"
            );


        if (specValueProperty == null)
        {
            throw new Exception(
                "SpecValue property was not found for " +
                label +
                "."
            );
        }


        specValueProperty.SetValue(
            spec,
            value
        );


        double verified =
            Convert.ToDouble(
                specValueProperty.GetValue(
                    spec
                ),
                CultureInfo.InvariantCulture
            );


        if (Math.Abs(
            verified - value
        ) > 0.000001)
        {
            throw new Exception(
                label +
                " specification did not change correctly."
            );
        }
    }


    // =============================================================
    // FLEXIBLE SPEC VALUE
    // =============================================================

    static string SetSpecValueFlexible(
        IDictionary dictionary,
        string[] candidateKeys,
        double value,
        string label)
    {
        foreach (string key
                 in candidateKeys)
        {
            if (dictionary.Contains(key))
            {
                SetSpecValue(
                    dictionary,
                    key,
                    value,
                    label
                );


                return key;
            }
        }


        var actualKeys =
            string.Join(
                ", ",
                dictionary.Keys
                    .Cast<object>()
                    .Select(
                        k => "'" + k + "'"
                    )
            );


        throw new Exception(
            "None of the candidate keys for " +
            label +
            " were found: " +
            string.Join(
                ", ",
                candidateKeys
            ) +
            ". Actual available keys are: " +
            actualKeys
        );
    }


    // =============================================================
    // GET DOUBLE FROM METHOD
    // =============================================================

    static double GetDoubleMethod(
        object obj,
        string methodName)
    {
        var method =
            obj.GetType().GetMethod(
                methodName,
                Type.EmptyTypes
            );


        if (method == null)
        {
            throw new Exception(
                methodName +
                "() was not found on " +
                obj.GetType().FullName
            );
        }


        object value =
            method.Invoke(
                obj,
                null
            );


        return Convert.ToDouble(
            value,
            CultureInfo.InvariantCulture
        );
    }


    // =============================================================
    // GET DOUBLE PROPERTY OR METHOD
    // =============================================================

    static double GetDoubleProperty(
        object obj,
        string propertyName,
        string methodName)
    {
        var property =
            obj.GetType().GetProperty(
                propertyName
            );


        if (property != null &&
            property.CanRead)
        {
            object value =
                property.GetValue(
                    obj
                );


            if (value != null)
            {
                return Convert.ToDouble(
                    value,
                    CultureInfo.InvariantCulture
                );
            }
        }


        var method =
            obj.GetType().GetMethod(
                methodName,
                Type.EmptyTypes
            );


        if (method != null)
        {
            object value =
                method.Invoke(
                    obj,
                    null
                );


            return Convert.ToDouble(
                value,
                CultureInfo.InvariantCulture
            );
        }


        throw new Exception(
            "Could not read " +
            propertyName +
            " or " +
            methodName +
            "() from " +
            obj.GetType().FullName
        );
    }


    // =============================================================
    // FLEXIBLE DOUBLE ACCESSOR
    // =============================================================

    static (double, string) GetDoubleFlexible(
        object obj,
        (string propertyName,
         string methodName)[] candidates)
    {
        List<string> attempted =
            new List<string>();


        foreach (
            var (propertyName, methodName)
            in candidates)
        {
            try
            {
                double value =
                    GetDoubleProperty(
                        obj,
                        propertyName,
                        methodName
                    );


                return (
                    value,
                    propertyName +
                    "/" +
                    methodName
                );
            }
            catch
            {
                attempted.Add(
                    propertyName +
                    "/" +
                    methodName
                );
            }
        }


        throw new Exception(
            "Could not read a duty value from " +
            obj.GetType().FullName +
            ". Tried: " +
            string.Join(
                ", ",
                attempted
            )
        );
    }


    // =============================================================
    // GET OVERALL COMPOSITION
    // =============================================================

    static double[] GetComposition(
        object stream)
    {
        var method =
            stream.GetType().GetMethod(
                "GetOverallComposition",
                Type.EmptyTypes
            );


        if (method == null)
        {
            throw new Exception(
                "GetOverallComposition() was not found."
            );
        }


        object result =
            method.Invoke(
                stream,
                null
            );


        if (!(result is Array array))
        {
            throw new Exception(
                "Unexpected composition type: " +
                result?.GetType().FullName
            );
        }


        double[] composition =
            new double[array.Length];


        for (int i = 0;
             i < array.Length;
             i++)
        {
            composition[i] =
                Convert.ToDouble(
                    array.GetValue(i),
                    CultureInfo.InvariantCulture
                );
        }


        return composition;
    }


    // =============================================================
    // CSV NUMBER FORMAT
    // =============================================================

    static string CsvValue(
        double value)
    {
        return value.ToString(
            "G17",
            CultureInfo.InvariantCulture
        );
    }
}