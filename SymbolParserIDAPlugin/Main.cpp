#include "ida.hpp"
#include "idp.hpp"
#include "loader.hpp"
#include "struct.hpp"
#include "typeinf.hpp"

#include <array>
#include <string>
#include <vector>

int idaapi Init();
void idaapi Term();
void idaapi Run(int);

plugin_t PLUGIN =
{
    IDP_INTERFACE_VERSION,
    0,
    Init,
    Term,
    Run,
    nullptr,
    nullptr,
    "SymbolParser",
    nullptr
};

struct IDAStructure
{
    std::string m_name;
    std::string m_comment;
    size_t m_size;

    struct Member
    { 
        std::string m_name;
        std::string m_type;
        std::string m_comment;
        size_t m_size;
    };

    std::vector<Member> m_members;
};

bool idaapi LoadStructInfo(void*);
bool idaapi ExportStructInfo(void*);

std::vector<IDAStructure> GetStructsFromDb();
void WriteStructsToFile(const std::string& path, const std::vector<IDAStructure>& structs);

int idaapi Init()
{
    add_menu_item("File/Load file/", "SymbolParser struct info", nullptr, 0, LoadStructInfo, nullptr);
    add_menu_item("File/Produce file/", "SymbolParser struct info", nullptr, 0, ExportStructInfo, nullptr);
    return PLUGIN_KEEP;
}

void idaapi Term()
{
}

void idaapi Run(int)
{
    ExportStructInfo(nullptr);
}

bool idaapi LoadStructInfo(void*)
{
    const std::string location = askfile_c(0, "*.spst", "Open a file exported with the SymbolParser plugin.");
    return true;
}

bool idaapi ExportStructInfo(void*)
{
    const std::string location = askfile_c(1, "*.spst", "Export a file with the SymbolParser plugin.");
    WriteStructsToFile(location, GetStructsFromDb());
    return true;
}

std::vector<IDAStructure> GetStructsFromDb()
{
    std::vector<IDAStructure> structures;

    constexpr size_t bufferSize = 256;
    std::array<char, bufferSize> buffer;

    for (auto i = get_first_struc_idx(); i != -1; i = get_next_struc_idx(i))
    {
        IDAStructure newStruct;
        const struc_t* idaStruct = get_struc(get_struc_by_idx(i));

        get_struc_name(idaStruct->id, buffer.data(), bufferSize);
        newStruct.m_name = std::string(buffer.data());

        get_struc_cmt(idaStruct->id, true, buffer.data(), bufferSize);
        newStruct.m_comment = std::string(buffer.data());

        newStruct.m_size = get_struc_size(idaStruct->id);

        msg("Struct %d = %s (%s) [%d bytes]\n", i, newStruct.m_name.c_str(), newStruct.m_comment.c_str(), newStruct.m_size);

        size_t offset = 0;
        member_t* idaStructMember = get_member(idaStruct, offset);

        while (idaStructMember != nullptr)
        {
            IDAStructure::Member newMember;

            get_member_fullname(idaStructMember->id, buffer.data(), bufferSize);
            newMember.m_name = std::string(buffer.data());

            {
                tinfo_t typeInfo;
                get_member_tinfo2(idaStructMember, &typeInfo);

                qstring typeName;

                if (typeInfo.get_type_name(&typeName))
                {
                    newMember.m_type = std::string(typeName.c_str());
                }
                else
                {
                    newMember.m_type = "undefined";
                }
            }

            get_member_cmt(idaStructMember->id, true, buffer.data(), bufferSize);
            newMember.m_comment = std::string(buffer.data());

            newMember.m_size = get_member_size(idaStructMember);     
            offset += newMember.m_size;

            msg("   %s {%s} (%s) [%d bytes]\n", newMember.m_name.c_str(), newMember.m_type.c_str(), newMember.m_comment.c_str(), newMember.m_size);

            newStruct.m_members.push_back(std::move(newMember));
            idaStructMember = get_member(idaStruct, offset);
        }

        structures.push_back(std::move(newStruct));

    }

    return std::move(structures);
}

void WriteStructsToFile(const std::string& path, const std::vector<IDAStructure>& structs)
{
}