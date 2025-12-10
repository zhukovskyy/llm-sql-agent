using System.Data;
using Microsoft.Data.SqlClient;

namespace DatabaseDemo.Services
{
    public class DatabaseInitializer
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> InitializeDatabase()
        {
            try
            {
                _logger.LogInformation("Starting database initialization...");

                await DropTablesAsync();
                _logger.LogInformation("Tables dropped successfully");

                await CreateTablesAsync();
                _logger.LogInformation("Tables created successfully");

                await SeedDataAsync();
                _logger.LogInformation("Data seeded successfully");

                return (true, "✅ Database initialized successfully! Tables created and populated with test data.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database");
                return (false, $"❌ Database initialization failed: {ex.Message}");
            }
        }

        private async Task DropTablesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var dropSql = @"
                -- Drop tables in correct order (respecting foreign keys if any)
                IF OBJECT_ID('dbo.vehicle_maintenance_logs', 'U') IS NOT NULL
                    DROP TABLE dbo.vehicle_maintenance_logs;
                
                IF OBJECT_ID('dbo.inventory_items', 'U') IS NOT NULL
                    DROP TABLE dbo.inventory_items;
                
                IF OBJECT_ID('dbo.employees', 'U') IS NOT NULL
                    DROP TABLE dbo.employees;
            ";

            using var command = new SqlCommand(dropSql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task CreateTablesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var createSql = @"
                -- Table 1: employees
                CREATE TABLE dbo.employees (
                    id INT PRIMARY KEY IDENTITY(1,1),
                    full_name NVARCHAR(100) NOT NULL,
                    role NVARCHAR(50) NOT NULL,
                    department NVARCHAR(50) NOT NULL,
                    rfid_tag NVARCHAR(50) NOT NULL UNIQUE
                );

                -- Table 2: inventory_items
                CREATE TABLE dbo.inventory_items (
                    id INT PRIMARY KEY IDENTITY(1,1),
                    item_name NVARCHAR(100) NOT NULL,
                    stock_quantity INT NOT NULL DEFAULT 0,
                    last_updated_by NVARCHAR(100),
                    last_update_rfid NVARCHAR(50)
                );

                -- Table 3: vehicle_maintenance_logs
                CREATE TABLE dbo.vehicle_maintenance_logs (
                    id INT PRIMARY KEY IDENTITY(1,1),
                    vehicle_plate NVARCHAR(20) NOT NULL,
                    issue NVARCHAR(500) NOT NULL,
                    maintenance_date DATE NOT NULL,
                    technician_name NVARCHAR(100) NOT NULL,
                    technician_rfid NVARCHAR(50) NOT NULL
                );
            ";

            using var command = new SqlCommand(createSql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task SeedDataAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Seed employees (50 records)
            var employeesSql = @"
                INSERT INTO dbo.employees (full_name, role, department, rfid_tag) VALUES
                ('John Smith', 'Driver', 'Logistics', 'RFID001'),
                ('Maria Garcia', 'Technician', 'Maintenance', 'RFID002'),
                ('James Johnson', 'Manager', 'HR', 'RFID003'),
                ('Patricia Brown', 'Driver', 'Logistics', 'RFID004'),
                ('Michael Davis', 'Warehouse Worker', 'Warehouse', 'RFID005'),
                ('Jennifer Wilson', 'Technician', 'Maintenance', 'RFID006'),
                ('David Miller', 'Manager', 'Logistics', 'RFID007'),
                ('Linda Martinez', 'Driver', 'Logistics', 'RFID008'),
                ('Robert Anderson', 'Warehouse Worker', 'Warehouse', 'RFID009'),
                ('Barbara Taylor', 'Technician', 'Maintenance', 'RFID010'),
                ('William Thomas', 'Driver', 'Logistics', 'RFID011'),
                ('Elizabeth Jackson', 'Manager', 'Warehouse', 'RFID012'),
                ('Richard White', 'Driver', 'Logistics', 'RFID013'),
                ('Susan Harris', 'Technician', 'Maintenance', 'RFID014'),
                ('Joseph Martin', 'Warehouse Worker', 'Warehouse', 'RFID015'),
                ('Jessica Thompson', 'Driver', 'Logistics', 'RFID016'),
                ('Thomas Garcia', 'Manager', 'Maintenance', 'RFID017'),
                ('Sarah Martinez', 'Warehouse Worker', 'Warehouse', 'RFID018'),
                ('Charles Robinson', 'Driver', 'Logistics', 'RFID019'),
                ('Karen Clark', 'Technician', 'Maintenance', 'RFID020'),
                ('Christopher Rodriguez', 'Driver', 'Logistics', 'RFID021'),
                ('Nancy Lewis', 'Warehouse Worker', 'Warehouse', 'RFID022'),
                ('Daniel Lee', 'Manager', 'HR', 'RFID023'),
                ('Lisa Walker', 'Driver', 'Logistics', 'RFID024'),
                ('Matthew Hall', 'Technician', 'Maintenance', 'RFID025'),
                ('Betty Allen', 'Warehouse Worker', 'Warehouse', 'RFID026'),
                ('Mark Young', 'Driver', 'Logistics', 'RFID027'),
                ('Sandra Hernandez', 'Manager', 'Logistics', 'RFID028'),
                ('Donald King', 'Technician', 'Maintenance', 'RFID029'),
                ('Ashley Wright', 'Driver', 'Logistics', 'RFID030'),
                ('Steven Lopez', 'Warehouse Worker', 'Warehouse', 'RFID031'),
                ('Kimberly Hill', 'Driver', 'Logistics', 'RFID032'),
                ('Paul Scott', 'Technician', 'Maintenance', 'RFID033'),
                ('Donna Green', 'Manager', 'Warehouse', 'RFID034'),
                ('Andrew Adams', 'Driver', 'Logistics', 'RFID035'),
                ('Carol Baker', 'Warehouse Worker', 'Warehouse', 'RFID036'),
                ('Joshua Gonzalez', 'Technician', 'Maintenance', 'RFID037'),
                ('Michelle Nelson', 'Driver', 'Logistics', 'RFID038'),
                ('Kenneth Carter', 'Manager', 'Maintenance', 'RFID039'),
                ('Emily Mitchell', 'Warehouse Worker', 'Warehouse', 'RFID040'),
                ('Kevin Perez', 'Driver', 'Logistics', 'RFID041'),
                ('Deborah Roberts', 'Technician', 'Maintenance', 'RFID042'),
                ('Brian Turner', 'Driver', 'Logistics', 'RFID043'),
                ('Laura Phillips', 'Warehouse Worker', 'Warehouse', 'RFID044'),
                ('George Campbell', 'Manager', 'HR', 'RFID045'),
                ('Stephanie Parker', 'Driver', 'Logistics', 'RFID046'),
                ('Edward Evans', 'Technician', 'Maintenance', 'RFID047'),
                ('Rebecca Edwards', 'Warehouse Worker', 'Warehouse', 'RFID048'),
                ('Ronald Collins', 'Driver', 'Logistics', 'RFID049'),
                ('Helen Stewart', 'Manager', 'Logistics', 'RFID050');
            ";

            using (var command = new SqlCommand(employeesSql, connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            // Seed inventory_items (80 records)
            var inventorySql = @"
                INSERT INTO dbo.inventory_items (item_name, stock_quantity, last_updated_by, last_update_rfid) VALUES
                ('Engine Oil 5W-30', 150, 'Michael Davis', 'RFID005'),
                ('Air Filter', 85, 'Robert Anderson', 'RFID009'),
                ('Brake Pads', 120, 'Michael Davis', 'RFID005'),
                ('Spark Plugs', 200, 'Joseph Martin', 'RFID015'),
                ('Transmission Fluid', 95, 'Sarah Martinez', 'RFID018'),
                ('Coolant', 180, 'Nancy Lewis', 'RFID022'),
                ('Battery 12V', 45, 'Betty Allen', 'RFID026'),
                ('Windshield Wipers', 110, 'Steven Lopez', 'RFID031'),
                ('Fuel Filter', 90, 'Carol Baker', 'RFID036'),
                ('Oil Filter', 160, 'Emily Mitchell', 'RFID040'),
                ('Tire Set', 30, 'Laura Phillips', 'RFID044'),
                ('Headlight Bulb', 75, 'Rebecca Edwards', 'RFID048'),
                ('Radiator Hose', 55, 'Michael Davis', 'RFID005'),
                ('Belt Drive', 70, 'Robert Anderson', 'RFID009'),
                ('Alternator', 25, 'Joseph Martin', 'RFID015'),
                ('Starter Motor', 18, 'Sarah Martinez', 'RFID018'),
                ('Shock Absorber', 40, 'Nancy Lewis', 'RFID022'),
                ('Brake Disc', 65, 'Betty Allen', 'RFID026'),
                ('Clutch Kit', 22, 'Steven Lopez', 'RFID031'),
                ('Timing Belt', 50, 'Carol Baker', 'RFID036'),
                ('Water Pump', 35, 'Emily Mitchell', 'RFID040'),
                ('Fuel Pump', 28, 'Laura Phillips', 'RFID044'),
                ('Thermostat', 80, 'Rebecca Edwards', 'RFID048'),
                ('Ignition Coil', 60, 'Michael Davis', 'RFID005'),
                ('Oxygen Sensor', 45, 'Robert Anderson', 'RFID009'),
                ('Cabin Air Filter', 95, 'Joseph Martin', 'RFID015'),
                ('Power Steering Fluid', 105, 'Sarah Martinez', 'RFID018'),
                ('Brake Fluid', 130, 'Nancy Lewis', 'RFID022'),
                ('Antifreeze', 140, 'Betty Allen', 'RFID026'),
                ('Hydraulic Oil', 88, 'Steven Lopez', 'RFID031'),
                ('Grease Cartridge', 200, 'Carol Baker', 'RFID036'),
                ('Wheel Bearing', 55, 'Emily Mitchell', 'RFID040'),
                ('CV Joint', 32, 'Laura Phillips', 'RFID044'),
                ('Suspension Spring', 38, 'Rebecca Edwards', 'RFID048'),
                ('Exhaust Pipe', 20, 'Michael Davis', 'RFID005'),
                ('Muffler', 15, 'Robert Anderson', 'RFID009'),
                ('Catalytic Converter', 12, 'Joseph Martin', 'RFID015'),
                ('Door Mirror', 28, 'Sarah Martinez', 'RFID018'),
                ('Window Regulator', 35, 'Nancy Lewis', 'RFID022'),
                ('Door Lock Actuator', 42, 'Betty Allen', 'RFID026'),
                ('Horn', 50, 'Steven Lopez', 'RFID031'),
                ('Fuse Box', 25, 'Carol Baker', 'RFID036'),
                ('Relay Switch', 90, 'Emily Mitchell', 'RFID040'),
                ('Dashboard Panel', 18, 'Laura Phillips', 'RFID044'),
                ('Seat Belt', 40, 'Rebecca Edwards', 'RFID048'),
                ('Floor Mat Set', 55, 'Michael Davis', 'RFID005'),
                ('Truck Cover', 22, 'Robert Anderson', 'RFID009'),
                ('Cargo Net', 45, 'Joseph Martin', 'RFID015'),
                ('Tie Down Straps', 100, 'Sarah Martinez', 'RFID018'),
                ('Safety Cone Set', 30, 'Nancy Lewis', 'RFID022'),
                ('Emergency Kit', 25, 'Betty Allen', 'RFID026'),
                ('Fire Extinguisher', 35, 'Steven Lopez', 'RFID031'),
                ('First Aid Kit', 40, 'Carol Baker', 'RFID036'),
                ('Warning Triangle', 50, 'Emily Mitchell', 'RFID040'),
                ('Jump Cables', 28, 'Laura Phillips', 'RFID044'),
                ('Tool Kit', 32, 'Rebecca Edwards', 'RFID048'),
                ('Jack Stand', 20, 'Michael Davis', 'RFID005'),
                ('Hydraulic Jack', 15, 'Robert Anderson', 'RFID009'),
                ('Torque Wrench', 18, 'Joseph Martin', 'RFID015'),
                ('Socket Set', 25, 'Sarah Martinez', 'RFID018'),
                ('Screwdriver Set', 35, 'Nancy Lewis', 'RFID022'),
                ('Pliers Set', 30, 'Betty Allen', 'RFID026'),
                ('Hammer', 40, 'Steven Lopez', 'RFID031'),
                ('Measuring Tape', 45, 'Carol Baker', 'RFID036'),
                ('Safety Gloves', 150, 'Emily Mitchell', 'RFID040'),
                ('Safety Goggles', 120, 'Laura Phillips', 'RFID044'),
                ('Hard Hat', 60, 'Rebecca Edwards', 'RFID048'),
                ('Reflective Vest', 80, 'Michael Davis', 'RFID005'),
                ('Work Boots', 45, 'Robert Anderson', 'RFID009'),
                ('Ear Plugs', 200, 'Joseph Martin', 'RFID015'),
                ('Dust Mask', 150, 'Sarah Martinez', 'RFID018'),
                ('Cleaning Supplies', 95, 'Nancy Lewis', 'RFID022'),
                ('Degreaser', 75, 'Betty Allen', 'RFID026'),
                ('Shop Towels', 180, 'Steven Lopez', 'RFID031'),
                ('Pressure Washer Detergent', 60, 'Carol Baker', 'RFID036'),
                ('Polish Compound', 48, 'Emily Mitchell', 'RFID040'),
                ('Wax', 52, 'Laura Phillips', 'RFID044'),
                ('Glass Cleaner', 85, 'Rebecca Edwards', 'RFID048'),
                ('Interior Cleaner', 70, 'Michael Davis', 'RFID005'),
                ('Tire Shine', 65, 'Robert Anderson', 'RFID009');
            ";

            using (var command = new SqlCommand(inventorySql, connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            // Seed vehicle_maintenance_logs (100 records)
            var maintenanceSql = @"
                INSERT INTO dbo.vehicle_maintenance_logs (vehicle_plate, issue, maintenance_date, technician_name, technician_rfid) VALUES
                ('ABC-1234', 'Oil change and filter replacement', '2025-01-15', 'Maria Garcia', 'RFID002'),
                ('XYZ-5678', 'Brake pads replacement', '2025-01-16', 'Jennifer Wilson', 'RFID006'),
                ('DEF-9012', 'Engine diagnostic check', '2025-01-18', 'Barbara Taylor', 'RFID010'),
                ('GHI-3456', 'Tire rotation and alignment', '2025-01-20', 'Susan Harris', 'RFID014'),
                ('JKL-7890', 'Transmission fluid change', '2025-01-22', 'Karen Clark', 'RFID020'),
                ('MNO-2345', 'Battery replacement', '2025-01-24', 'Matthew Hall', 'RFID025'),
                ('PQR-6789', 'Air conditioning repair', '2025-01-26', 'Donald King', 'RFID029'),
                ('STU-0123', 'Suspension check and repair', '2025-01-28', 'Paul Scott', 'RFID033'),
                ('VWX-4567', 'Exhaust system repair', '2025-01-30', 'Joshua Gonzalez', 'RFID037'),
                ('YZA-8901', 'Headlight bulb replacement', '2025-02-01', 'Deborah Roberts', 'RFID042'),
                ('BCD-2345', 'Windshield wiper replacement', '2025-02-03', 'Edward Evans', 'RFID047'),
                ('EFG-6789', 'Fuel filter replacement', '2025-02-05', 'Maria Garcia', 'RFID002'),
                ('HIJ-0123', 'Coolant flush and refill', '2025-02-07', 'Jennifer Wilson', 'RFID006'),
                ('KLM-4567', 'Timing belt replacement', '2025-02-09', 'Barbara Taylor', 'RFID010'),
                ('NOP-8901', 'Spark plugs replacement', '2025-02-11', 'Susan Harris', 'RFID014'),
                ('QRS-2345', 'Alternator replacement', '2025-02-13', 'Karen Clark', 'RFID020'),
                ('TUV-6789', 'Starter motor repair', '2025-02-15', 'Matthew Hall', 'RFID025'),
                ('WXY-0123', 'Radiator replacement', '2025-02-17', 'Donald King', 'RFID029'),
                ('ZAB-4567', 'Clutch replacement', '2025-02-19', 'Paul Scott', 'RFID033'),
                ('CDE-8901', 'Water pump replacement', '2025-02-21', 'Joshua Gonzalez', 'RFID037'),
                ('FGH-2345', 'Oxygen sensor replacement', '2025-02-23', 'Deborah Roberts', 'RFID042'),
                ('IJK-6789', 'Catalytic converter replacement', '2025-02-25', 'Edward Evans', 'RFID047'),
                ('LMN-0123', 'Power steering pump repair', '2025-02-27', 'Maria Garcia', 'RFID002'),
                ('OPQ-4567', 'Wheel bearing replacement', '2025-03-01', 'Jennifer Wilson', 'RFID006'),
                ('RST-8901', 'CV joint replacement', '2025-03-03', 'Barbara Taylor', 'RFID010'),
                ('UVW-2345', 'Shock absorber replacement', '2025-03-05', 'Susan Harris', 'RFID014'),
                ('XYZ-6789', 'Door lock actuator repair', '2025-03-07', 'Karen Clark', 'RFID020'),
                ('ABC-0123', 'Window regulator replacement', '2025-03-09', 'Matthew Hall', 'RFID025'),
                ('DEF-4567', 'Mirror replacement', '2025-03-11', 'Donald King', 'RFID029'),
                ('GHI-8901', 'Horn replacement', '2025-03-13', 'Paul Scott', 'RFID033'),
                ('JKL-2345', 'Fuse box inspection', '2025-03-15', 'Joshua Gonzalez', 'RFID037'),
                ('MNO-6789', 'Engine oil leak repair', '2025-03-17', 'Deborah Roberts', 'RFID042'),
                ('PQR-0123', 'Transmission leak repair', '2025-03-19', 'Edward Evans', 'RFID047'),
                ('STU-4567', 'Brake fluid flush', '2025-03-21', 'Maria Garcia', 'RFID002'),
                ('VWX-8901', 'Power steering fluid flush', '2025-03-23', 'Jennifer Wilson', 'RFID006'),
                ('YZA-2345', 'Differential oil change', '2025-03-25', 'Barbara Taylor', 'RFID010'),
                ('BCD-6789', 'Air filter replacement', '2025-03-27', 'Susan Harris', 'RFID014'),
                ('EFG-0123', 'Cabin air filter replacement', '2025-03-29', 'Karen Clark', 'RFID020'),
                ('HIJ-4567', 'Throttle body cleaning', '2025-03-31', 'Matthew Hall', 'RFID025'),
                ('KLM-8901', 'Fuel injector cleaning', '2025-04-02', 'Donald King', 'RFID029'),
                ('NOP-2345', 'EGR valve cleaning', '2025-04-04', 'Paul Scott', 'RFID033'),
                ('QRS-6789', 'PCV valve replacement', '2025-04-06', 'Joshua Gonzalez', 'RFID037'),
                ('TUV-0123', 'Serpentine belt replacement', '2025-04-08', 'Deborah Roberts', 'RFID042'),
                ('WXY-4567', 'Drive belt tensioner replacement', '2025-04-10', 'Edward Evans', 'RFID047'),
                ('ZAB-8901', 'Idler pulley replacement', '2025-04-12', 'Maria Garcia', 'RFID002'),
                ('CDE-2345', 'Thermostat replacement', '2025-04-14', 'Jennifer Wilson', 'RFID006'),
                ('FGH-6789', 'Radiator hose replacement', '2025-04-16', 'Barbara Taylor', 'RFID010'),
                ('IJK-0123', 'Heater core flush', '2025-04-18', 'Susan Harris', 'RFID014'),
                ('LMN-4567', 'Blower motor replacement', '2025-04-20', 'Karen Clark', 'RFID020'),
                ('OPQ-8901', 'AC compressor replacement', '2025-04-22', 'Matthew Hall', 'RFID025'),
                ('RST-2345', 'AC condenser replacement', '2025-04-24', 'Donald King', 'RFID029'),
                ('UVW-6789', 'AC evaporator replacement', '2025-04-26', 'Paul Scott', 'RFID033'),
                ('XYZ-0123', 'Refrigerant recharge', '2025-04-28', 'Joshua Gonzalez', 'RFID037'),
                ('ABC-4567', 'Steering rack replacement', '2025-04-30', 'Deborah Roberts', 'RFID042'),
                ('DEF-8901', 'Tie rod end replacement', '2025-05-02', 'Edward Evans', 'RFID047'),
                ('GHI-2345', 'Ball joint replacement', '2025-05-04', 'Maria Garcia', 'RFID002'),
                ('JKL-6789', 'Control arm bushing replacement', '2025-05-06', 'Jennifer Wilson', 'RFID006'),
                ('MNO-0123', 'Sway bar link replacement', '2025-05-08', 'Barbara Taylor', 'RFID010'),
                ('PQR-4567', 'Strut mount replacement', '2025-05-10', 'Susan Harris', 'RFID014'),
                ('STU-8901', 'Engine mount replacement', '2025-05-12', 'Karen Clark', 'RFID020'),
                ('VWX-2345', 'Transmission mount replacement', '2025-05-14', 'Matthew Hall', 'RFID025'),
                ('YZA-6789', 'Exhaust manifold gasket replacement', '2025-05-16', 'Donald King', 'RFID029'),
                ('BCD-0123', 'Valve cover gasket replacement', '2025-05-18', 'Paul Scott', 'RFID033'),
                ('EFG-4567', 'Oil pan gasket replacement', '2025-05-20', 'Joshua Gonzalez', 'RFID037'),
                ('HIJ-8901', 'Head gasket replacement', '2025-05-22', 'Deborah Roberts', 'RFID042'),
                ('KLM-2345', 'Turbocharger replacement', '2025-05-24', 'Edward Evans', 'RFID047'),
                ('NOP-6789', 'Intercooler cleaning', '2025-05-26', 'Maria Garcia', 'RFID002'),
                ('QRS-0123', 'Mass airflow sensor cleaning', '2025-05-28', 'Jennifer Wilson', 'RFID006'),
                ('TUV-4567', 'Throttle position sensor replacement', '2025-05-30', 'Barbara Taylor', 'RFID010'),
                ('WXY-8901', 'Camshaft position sensor replacement', '2025-06-01', 'Susan Harris', 'RFID014'),
                ('ZAB-2345', 'Crankshaft position sensor replacement', '2025-06-03', 'Karen Clark', 'RFID020'),
                ('CDE-6789', 'Knock sensor replacement', '2025-06-05', 'Matthew Hall', 'RFID025'),
                ('FGH-0123', 'Coolant temperature sensor replacement', '2025-06-07', 'Donald King', 'RFID029'),
                ('IJK-4567', 'Oil pressure sensor replacement', '2025-06-09', 'Paul Scott', 'RFID033'),
                ('LMN-8901', 'Fuel pressure regulator replacement', '2025-06-11', 'Joshua Gonzalez', 'RFID037'),
                ('OPQ-2345', 'EVAP canister replacement', '2025-06-13', 'Deborah Roberts', 'RFID042'),
                ('RST-6789', 'Purge valve replacement', '2025-06-15', 'Edward Evans', 'RFID047'),
                ('UVW-0123', 'Gas cap replacement', '2025-06-17', 'Maria Garcia', 'RFID002'),
                ('XYZ-4567', 'Differential fluid change', '2025-06-19', 'Jennifer Wilson', 'RFID006'),
                ('ABC-8901', 'Transfer case fluid change', '2025-06-21', 'Barbara Taylor', 'RFID010'),
                ('DEF-2345', 'Four-wheel drive actuator repair', '2025-06-23', 'Susan Harris', 'RFID014'),
                ('GHI-6789', 'Hub assembly replacement', '2025-06-25', 'Karen Clark', 'RFID020'),
                ('JKL-0123', 'ABS sensor replacement', '2025-06-27', 'Matthew Hall', 'RFID025'),
                ('MNO-4567', 'ABS module replacement', '2025-06-29', 'Donald King', 'RFID029'),
                ('PQR-8901', 'Brake master cylinder replacement', '2025-07-01', 'Paul Scott', 'RFID033'),
                ('STU-2345', 'Brake booster replacement', '2025-07-03', 'Joshua Gonzalez', 'RFID037'),
                ('VWX-6789', 'Brake caliper replacement', '2025-07-05', 'Deborah Roberts', 'RFID042'),
                ('YZA-0123', 'Brake rotor resurfacing', '2025-07-07', 'Edward Evans', 'RFID047'),
                ('BCD-4567', 'Brake line replacement', '2025-07-09', 'Maria Garcia', 'RFID002'),
                ('EFG-8901', 'Emergency brake cable replacement', '2025-07-11', 'Jennifer Wilson', 'RFID006'),
                ('HIJ-2345', 'Wheel alignment', '2025-07-13', 'Barbara Taylor', 'RFID010'),
                ('KLM-6789', 'Wheel balancing', '2025-07-15', 'Susan Harris', 'RFID014'),
                ('NOP-0123', 'Tire pressure monitoring system repair', '2025-07-17', 'Karen Clark', 'RFID020'),
                ('QRS-4567', 'Tire rotation', '2025-07-19', 'Matthew Hall', 'RFID025'),
                ('TUV-8901', 'Tire replacement - all four', '2025-07-21', 'Donald King', 'RFID029'),
                ('WXY-2345', 'Spare tire inspection', '2025-07-23', 'Paul Scott', 'RFID033'),
                ('ZAB-6789', 'Jack and tools inspection', '2025-07-25', 'Joshua Gonzalez', 'RFID037'),
                ('CDE-0123', 'Annual safety inspection', '2025-07-27', 'Deborah Roberts', 'RFID042'),
                ('FGH-4567', 'Emissions test and repair', '2025-07-29', 'Edward Evans', 'RFID047');
            ";

            using (var command = new SqlCommand(maintenanceSql, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
