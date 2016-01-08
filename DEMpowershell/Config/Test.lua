

local function TestException()
	error("Test Error.");
end;

function ThrowException()
	TestException();
end;


Actions["ex"] = {
	Security = "",
	Description = "Throw exception",
	SafeCall = true,
	Method = ThrowException
};